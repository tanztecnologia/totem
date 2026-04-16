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
    StockBaseUnit? StockBaseUnit,
    decimal? StockOnHandBaseQty,
    bool IsActive
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

        if (name.Length < 2) throw new ArgumentException("Name inválido.");
        if (command.PriceCents < 0) throw new ArgumentException("PriceCents inválido.");
        if (command.AveragePrepSeconds is not null && command.AveragePrepSeconds <= 0)
            throw new ArgumentException("AveragePrepSeconds inválido.");
        if (stockOnHand is not null && stockOnHand < 0) throw new ArgumentException("StockOnHandBaseQty inválido.");
        if (command.StockBaseUnit is null && stockOnHand is not null)
            throw new ArgumentException("StockBaseUnit é obrigatório quando StockOnHandBaseQty for informado.");
        if (command.StockBaseUnit is not null && stockOnHand is null)
            stockOnHand = 0;

        var category = await _categories.GetByCodeAsync(command.TenantId, categoryCode, ct);
        if (category is null) throw new InvalidOperationException("Categoria não encontrada.");

        var now = DateTimeOffset.UtcNow;
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var max = await _skus.GetMaxCodeNumberAsync(command.TenantId, ct);
            var nextCode = (max + 1).ToString("D5");

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
                StockBaseUnit: command.StockBaseUnit,
                StockOnHandBaseQty: stockOnHand,
                IsActive: command.IsActive,
                CreatedAt: now,
                UpdatedAt: now
            );

            try
            {
                await _skus.AddAsync(sku, ct);
                return new SkuResult(
                    Id: sku.Id,
                    TenantId: sku.TenantId,
                    CategoryCode: sku.CategoryCode,
                    Code: sku.Code,
                    Name: sku.Name,
                    PriceCents: sku.PriceCents,
                    AveragePrepSeconds: sku.AveragePrepSeconds,
                    ImageUrl: sku.ImageUrl,
                    NfeCProd: sku.NfeCProd,
                    NfeCEan: sku.NfeCEan,
                    NfeCfop: sku.NfeCfop,
                    NfeUCom: sku.NfeUCom,
                    NfeQCom: sku.NfeQCom,
                    NfeVUnCom: sku.NfeVUnCom,
                    NfeVProd: sku.NfeVProd,
                    NfeCEanTrib: sku.NfeCEanTrib,
                    NfeUTrib: sku.NfeUTrib,
                    NfeQTrib: sku.NfeQTrib,
                    NfeVUnTrib: sku.NfeVUnTrib,
                    NfeIcmsOrig: sku.NfeIcmsOrig,
                    NfeIcmsCst: sku.NfeIcmsCst,
                    NfeIcmsModBc: sku.NfeIcmsModBc,
                    NfeIcmsVBc: sku.NfeIcmsVBc,
                    NfeIcmsPIcms: sku.NfeIcmsPIcms,
                    NfeIcmsVIcms: sku.NfeIcmsVIcms,
                    NfePisCst: sku.NfePisCst,
                    NfePisVBc: sku.NfePisVBc,
                    NfePisPPis: sku.NfePisPPis,
                    NfePisVPis: sku.NfePisVPis,
                    NfeCofinsCst: sku.NfeCofinsCst,
                    NfeCofinsVBc: sku.NfeCofinsVBc,
                    NfeCofinsPCofins: sku.NfeCofinsPCofins,
                    NfeCofinsVCofins: sku.NfeCofinsVCofins,
                    StockBaseUnit: sku.StockBaseUnit,
                    StockOnHandBaseQty: sku.StockOnHandBaseQty,
                    IsActive: sku.IsActive
                );
            }
            catch (DbUpdateException) when (attempt < 2)
            {
            }
        }

        throw new InvalidOperationException("Falha ao gerar código do SKU.");
    }
}
