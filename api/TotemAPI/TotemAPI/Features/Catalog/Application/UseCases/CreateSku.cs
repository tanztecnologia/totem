using Microsoft.EntityFrameworkCore;
using TotemAPI.Features.Catalog.Application.Abstractions;
using TotemAPI.Features.Catalog.Domain;

namespace TotemAPI.Features.Catalog.Application.UseCases;

public sealed record CreateSkuCommand(
    Guid TenantId,
    string CategoryCode,
    string Name,
    int PriceCents,
    int? AveragePrepSeconds,
    string? ImageUrl,
    bool? TracksStock,
    StockBaseUnit? StockBaseUnit,
    decimal? StockOnHandBaseQty,
    bool IsActive,
    Guid? ActorUserId = null
);

public sealed record SkuResult(
    Guid Id,
    Guid TenantId,
    string CategoryCode,
    string Code,
    string Name,
    int PriceCents,
    int? AveragePrepSeconds,
    string? ImageUrl,
    string? NfeCProd,
    string? NfeCEan,
    string? NfeCfop,
    string? NfeUCom,
    decimal? NfeQCom,
    decimal? NfeVUnCom,
    decimal? NfeVProd,
    string? NfeCEanTrib,
    string? NfeUTrib,
    decimal? NfeQTrib,
    decimal? NfeVUnTrib,
    string? NfeIcmsOrig,
    string? NfeIcmsCst,
    string? NfeIcmsModBc,
    decimal? NfeIcmsVBc,
    decimal? NfeIcmsPIcms,
    decimal? NfeIcmsVIcms,
    string? NfePisCst,
    decimal? NfePisVBc,
    decimal? NfePisPPis,
    decimal? NfePisVPis,
    string? NfeCofinsCst,
    decimal? NfeCofinsVBc,
    decimal? NfeCofinsPCofins,
    decimal? NfeCofinsVCofins,
    bool TracksStock,
    StockBaseUnit? StockBaseUnit,
    decimal? StockOnHandBaseQty,
    bool IsActive
);

public sealed class CreateSku
{
    public CreateSku(ISkuRepository skus, ICategoryRepository categories)
    {
        _skus = skus;
        _categories = categories;
    }

    private readonly ISkuRepository _skus;
    private readonly ICategoryRepository _categories;

    public async Task<SkuResult> HandleAsync(CreateSkuCommand command, CancellationToken ct)
    {
        if (command.TenantId == Guid.Empty) throw new ArgumentException("TenantId inválido.");
        var categoryCode = (command.CategoryCode ?? string.Empty).Trim();
        if (categoryCode.Length < 1) throw new ArgumentException("CategoryCode inválido.");

        var name = (command.Name ?? string.Empty).Trim();
        var imageUrl = string.IsNullOrWhiteSpace(command.ImageUrl) ? null : command.ImageUrl.Trim();
        var stockOnHand = command.StockOnHandBaseQty;
        var tracksStock = command.TracksStock ?? (command.StockBaseUnit is not null || stockOnHand is not null);

        if (name.Length < 2) throw new ArgumentException("Name inválido.");
        if (command.PriceCents < 0) throw new ArgumentException("PriceCents inválido.");
        if (command.AveragePrepSeconds is not null && command.AveragePrepSeconds <= 0)
            throw new ArgumentException("AveragePrepSeconds inválido.");
        if (!tracksStock)
        {
            if (command.StockBaseUnit is not null || stockOnHand is not null)
                throw new ArgumentException("StockBaseUnit/StockOnHandBaseQty não devem ser informados quando TracksStock for false.");
        }
        else
        {
            if (stockOnHand is not null && stockOnHand < 0) throw new ArgumentException("StockOnHandBaseQty inválido.");
            if (command.StockBaseUnit is null && stockOnHand is not null)
                throw new ArgumentException("StockBaseUnit é obrigatório quando StockOnHandBaseQty for informado.");
            if (command.StockBaseUnit is not null && stockOnHand is null)
                stockOnHand = 0;
        }

        var category = await _categories.GetByCodeAsync(command.TenantId, categoryCode, ct);
        if (category is null) throw new InvalidOperationException("Categoria não encontrada.");

        var now = DateTimeOffset.UtcNow;
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var max = await _skus.GetMaxCodeNumberAsync(command.TenantId, ct);
            var nextCode = (max + 1).ToString("D5");

            var initialOnHand = tracksStock ? stockOnHand ?? 0m : (decimal?)null;
            var sku = new Sku(
                Id: Guid.NewGuid(),
                TenantId: command.TenantId,
                CategoryCode: category.Code,
                Code: nextCode,
                Name: name,
                PriceCents: command.PriceCents,
                AveragePrepSeconds: command.AveragePrepSeconds,
                ImageUrl: imageUrl,
                NfeCProd: null,
                NfeCEan: null,
                NfeCfop: null,
                NfeUCom: null,
                NfeQCom: null,
                NfeVUnCom: null,
                NfeVProd: null,
                NfeCEanTrib: null,
                NfeUTrib: null,
                NfeQTrib: null,
                NfeVUnTrib: null,
                NfeIcmsOrig: null,
                NfeIcmsCst: null,
                NfeIcmsModBc: null,
                NfeIcmsVBc: null,
                NfeIcmsPIcms: null,
                NfeIcmsVIcms: null,
                NfePisCst: null,
                NfePisVBc: null,
                NfePisPPis: null,
                NfePisVPis: null,
                NfeCofinsCst: null,
                NfeCofinsVBc: null,
                NfeCofinsPCofins: null,
                NfeCofinsVCofins: null,
                TracksStock: tracksStock,
                StockBaseUnit: tracksStock ? command.StockBaseUnit : null,
                StockOnHandBaseQty: tracksStock && command.StockBaseUnit is not null ? 0m : null,
                IsActive: command.IsActive,
                CreatedAt: now,
                UpdatedAt: now
            );

            try
            {
                await _skus.AddAsync(sku, ct);

                if (tracksStock && command.StockBaseUnit is not null && initialOnHand > 0)
                {
                    await _skus.AddStockLedgerEntryAsync(new SkuStockLedgerEntry(
                        Id: Guid.NewGuid(),
                        TenantId: sku.TenantId,
                        SkuId: sku.Id,
                        DeltaBaseQty: initialOnHand.Value,
                        StockAfterBaseQty: 0, // calculado pelo repositório
                        OriginType: StockLedgerOriginType.InitialStock,
                        OriginId: null,
                        Notes: "Estoque inicial",
                        ActorUserId: command.ActorUserId,
                        CreatedAt: now
                    ), ct);
                }
                var created = await _skus.GetByIdAsync(sku.TenantId, sku.Id, ct) ?? sku;
                return new SkuResult(
                    Id: created.Id,
                    TenantId: created.TenantId,
                    CategoryCode: created.CategoryCode,
                    Code: created.Code,
                    Name: created.Name,
                    PriceCents: created.PriceCents,
                    AveragePrepSeconds: created.AveragePrepSeconds,
                    ImageUrl: created.ImageUrl,
                    NfeCProd: created.NfeCProd,
                    NfeCEan: created.NfeCEan,
                    NfeCfop: created.NfeCfop,
                    NfeUCom: created.NfeUCom,
                    NfeQCom: created.NfeQCom,
                    NfeVUnCom: created.NfeVUnCom,
                    NfeVProd: created.NfeVProd,
                    NfeCEanTrib: created.NfeCEanTrib,
                    NfeUTrib: created.NfeUTrib,
                    NfeQTrib: created.NfeQTrib,
                    NfeVUnTrib: created.NfeVUnTrib,
                    NfeIcmsOrig: created.NfeIcmsOrig,
                    NfeIcmsCst: created.NfeIcmsCst,
                    NfeIcmsModBc: created.NfeIcmsModBc,
                    NfeIcmsVBc: created.NfeIcmsVBc,
                    NfeIcmsPIcms: created.NfeIcmsPIcms,
                    NfeIcmsVIcms: created.NfeIcmsVIcms,
                    NfePisCst: created.NfePisCst,
                    NfePisVBc: created.NfePisVBc,
                    NfePisPPis: created.NfePisPPis,
                    NfePisVPis: created.NfePisVPis,
                    NfeCofinsCst: created.NfeCofinsCst,
                    NfeCofinsVBc: created.NfeCofinsVBc,
                    NfeCofinsPCofins: created.NfeCofinsPCofins,
                    NfeCofinsVCofins: created.NfeCofinsVCofins,
                    TracksStock: created.TracksStock,
                    StockBaseUnit: created.StockBaseUnit,
                    StockOnHandBaseQty: created.StockOnHandBaseQty,
                    IsActive: created.IsActive
                );
            }
            catch (DbUpdateException) when (attempt < 2)
            {
            }
        }

        throw new InvalidOperationException("Falha ao gerar código do SKU.");
    }
}
