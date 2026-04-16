using TotemAPI.Features.Catalog.Application.Abstractions;
using TotemAPI.Features.Catalog.Domain;

namespace TotemAPI.Features.Catalog.Application.UseCases;

public sealed record UpdateSkuCommand(
    Guid TenantId,
    Guid SkuId,
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

public sealed class UpdateSku
{
    public UpdateSku(ISkuRepository skus, ICategoryRepository categories)
    {
        _skus = skus;
        _categories = categories;
    }

    private readonly ISkuRepository _skus;
    private readonly ICategoryRepository _categories;

    public async Task<SkuResult?> HandleAsync(UpdateSkuCommand command, CancellationToken ct)
    {
        if (command.TenantId == Guid.Empty) throw new ArgumentException("TenantId inválido.");
        if (command.SkuId == Guid.Empty) throw new ArgumentException("SkuId inválido.");
        var categoryCode = (command.CategoryCode ?? string.Empty).Trim();
        if (categoryCode.Length < 1) throw new ArgumentException("CategoryCode inválido.");

        var name = (command.Name ?? string.Empty).Trim();
        var imageUrl = string.IsNullOrWhiteSpace(command.ImageUrl) ? null : command.ImageUrl.Trim();

        if (name.Length < 2) throw new ArgumentException("Name inválido.");
        if (command.PriceCents < 0) throw new ArgumentException("PriceCents inválido.");
        if (command.AveragePrepSeconds is not null && command.AveragePrepSeconds <= 0)
            throw new ArgumentException("AveragePrepSeconds inválido.");

        var current = await _skus.GetByIdAsync(command.TenantId, command.SkuId, ct);
        if (current is null) return null;

        var category = await _categories.GetByCodeAsync(command.TenantId, categoryCode, ct);
        if (category is null) throw new InvalidOperationException("Categoria não encontrada.");

        var nextTracksStock = command.TracksStock ?? current.TracksStock;
        var nextStockBaseUnit = command.StockBaseUnit ?? current.StockBaseUnit;

        decimal? stockOnHandForRow;
        SkuStockLedgerEntry? ledgerEntry = null;
        var now = DateTimeOffset.UtcNow;

        if (!nextTracksStock)
        {
            if (command.StockBaseUnit is not null || command.StockOnHandBaseQty is not null)
                throw new ArgumentException("StockBaseUnit/StockOnHandBaseQty não devem ser informados quando TracksStock for false.");
            nextStockBaseUnit = null;
            stockOnHandForRow = null;
        }
        else
        {
            if (command.StockBaseUnit is not null && command.StockOnHandBaseQty is null && current.StockBaseUnit != nextStockBaseUnit)
                throw new ArgumentException("StockOnHandBaseQty é obrigatório quando StockBaseUnit for alterado.");
            if (command.StockOnHandBaseQty is not null && command.StockOnHandBaseQty < 0)
                throw new ArgumentException("StockOnHandBaseQty inválido.");
            if (nextStockBaseUnit is null && command.StockOnHandBaseQty is not null)
                throw new ArgumentException("StockBaseUnit é obrigatório quando StockOnHandBaseQty for informado.");

            var wasTracking = current.TracksStock && current.StockBaseUnit is not null;
            var unitChanged = wasTracking && command.StockBaseUnit is not null && command.StockBaseUnit != current.StockBaseUnit;
            var firstEnable = !current.TracksStock && nextTracksStock;

            if (firstEnable || unitChanged)
            {
                // Reinicialização: zera no row para o ledger aplicar o delta correto
                stockOnHandForRow = nextStockBaseUnit is not null ? 0m : null;
                var providedQty = command.StockOnHandBaseQty ?? 0m;
                if (nextStockBaseUnit is not null && providedQty > 0)
                {
                    ledgerEntry = new SkuStockLedgerEntry(
                        Id: Guid.NewGuid(),
                        TenantId: current.TenantId,
                        SkuId: current.Id,
                        DeltaBaseQty: providedQty,
                        StockAfterBaseQty: 0,
                        OriginType: firstEnable ? StockLedgerOriginType.InitialStock : StockLedgerOriginType.ManualAdjustment,
                        OriginId: null,
                        Notes: firstEnable ? "Estoque inicial (via atualização de SKU)" : "Ajuste por mudança de unidade de estoque",
                        ActorUserId: command.ActorUserId,
                        CreatedAt: now
                    );
                }
            }
            else if (wasTracking && command.StockOnHandBaseQty is not null &&
                     command.StockOnHandBaseQty != current.StockOnHandBaseQty)
            {
                // Ajuste manual de quantidade com mesma unidade
                var delta = command.StockOnHandBaseQty.Value - (current.StockOnHandBaseQty ?? 0m);
                stockOnHandForRow = current.StockOnHandBaseQty; // mantém; ledger atualiza
                if (delta != 0)
                {
                    ledgerEntry = new SkuStockLedgerEntry(
                        Id: Guid.NewGuid(),
                        TenantId: current.TenantId,
                        SkuId: current.Id,
                        DeltaBaseQty: delta,
                        StockAfterBaseQty: 0,
                        OriginType: StockLedgerOriginType.ManualAdjustment,
                        OriginId: null,
                        Notes: "Ajuste manual de estoque",
                        ActorUserId: command.ActorUserId,
                        CreatedAt: now
                    );
                }
                else
                {
                    stockOnHandForRow = current.StockOnHandBaseQty;
                }
            }
            else
            {
                // Sem mudança de quantidade — preserva o valor projetado
                stockOnHandForRow = current.StockOnHandBaseQty;
            }
        }

        var updated = new Sku(
            Id: current.Id,
            TenantId: current.TenantId,
            CategoryCode: category.Code,
            Code: current.Code,
            Name: name,
            PriceCents: command.PriceCents,
            AveragePrepSeconds: command.AveragePrepSeconds,
            ImageUrl: imageUrl,
            NfeCProd: current.NfeCProd,
            NfeCEan: current.NfeCEan,
            NfeCfop: current.NfeCfop,
            NfeUCom: current.NfeUCom,
            NfeQCom: current.NfeQCom,
            NfeVUnCom: current.NfeVUnCom,
            NfeVProd: current.NfeVProd,
            NfeCEanTrib: current.NfeCEanTrib,
            NfeUTrib: current.NfeUTrib,
            NfeQTrib: current.NfeQTrib,
            NfeVUnTrib: current.NfeVUnTrib,
            NfeIcmsOrig: current.NfeIcmsOrig,
            NfeIcmsCst: current.NfeIcmsCst,
            NfeIcmsModBc: current.NfeIcmsModBc,
            NfeIcmsVBc: current.NfeIcmsVBc,
            NfeIcmsPIcms: current.NfeIcmsPIcms,
            NfeIcmsVIcms: current.NfeIcmsVIcms,
            NfePisCst: current.NfePisCst,
            NfePisVBc: current.NfePisVBc,
            NfePisPPis: current.NfePisPPis,
            NfePisVPis: current.NfePisVPis,
            NfeCofinsCst: current.NfeCofinsCst,
            NfeCofinsVBc: current.NfeCofinsVBc,
            NfeCofinsPCofins: current.NfeCofinsPCofins,
            NfeCofinsVCofins: current.NfeCofinsVCofins,
            TracksStock: nextTracksStock,
            StockBaseUnit: nextStockBaseUnit,
            StockOnHandBaseQty: stockOnHandForRow,
            IsActive: command.IsActive,
            CreatedAt: current.CreatedAt,
            UpdatedAt: now
        );

        await _skus.UpdateAsync(updated, ct);
        if (ledgerEntry is not null)
            await _skus.AddStockLedgerEntryAsync(ledgerEntry, ct);

        var final = await _skus.GetByIdAsync(command.TenantId, command.SkuId, ct) ?? updated;
        return new SkuResult(
            Id: final.Id,
            TenantId: final.TenantId,
            CategoryCode: final.CategoryCode,
            Code: final.Code,
            Name: final.Name,
            PriceCents: final.PriceCents,
            AveragePrepSeconds: final.AveragePrepSeconds,
            ImageUrl: final.ImageUrl,
            NfeCProd: final.NfeCProd,
            NfeCEan: final.NfeCEan,
            NfeCfop: final.NfeCfop,
            NfeUCom: final.NfeUCom,
            NfeQCom: final.NfeQCom,
            NfeVUnCom: final.NfeVUnCom,
            NfeVProd: final.NfeVProd,
            NfeCEanTrib: final.NfeCEanTrib,
            NfeUTrib: final.NfeUTrib,
            NfeQTrib: final.NfeQTrib,
            NfeVUnTrib: final.NfeVUnTrib,
            NfeIcmsOrig: final.NfeIcmsOrig,
            NfeIcmsCst: final.NfeIcmsCst,
            NfeIcmsModBc: final.NfeIcmsModBc,
            NfeIcmsVBc: final.NfeIcmsVBc,
            NfeIcmsPIcms: final.NfeIcmsPIcms,
            NfeIcmsVIcms: final.NfeIcmsVIcms,
            NfePisCst: final.NfePisCst,
            NfePisVBc: final.NfePisVBc,
            NfePisPPis: final.NfePisPPis,
            NfePisVPis: final.NfePisVPis,
            NfeCofinsCst: final.NfeCofinsCst,
            NfeCofinsVBc: final.NfeCofinsVBc,
            NfeCofinsPCofins: final.NfeCofinsPCofins,
            NfeCofinsVCofins: final.NfeCofinsVCofins,
            TracksStock: final.TracksStock,
            StockBaseUnit: final.StockBaseUnit,
            StockOnHandBaseQty: final.StockOnHandBaseQty,
            IsActive: final.IsActive
        );
    }
}
