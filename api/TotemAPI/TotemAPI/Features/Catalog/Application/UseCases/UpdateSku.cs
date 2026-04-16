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
    StockBaseUnit? StockBaseUnit,
    decimal? StockOnHandBaseQty,
    bool IsActive
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

        var nextStockBaseUnit = command.StockBaseUnit ?? current.StockBaseUnit;
        var nextStockOnHand = command.StockOnHandBaseQty ?? current.StockOnHandBaseQty;
        if (command.StockBaseUnit is not null && command.StockOnHandBaseQty is null && current.StockBaseUnit != nextStockBaseUnit)
            throw new ArgumentException("StockOnHandBaseQty é obrigatório quando StockBaseUnit for alterado.");
        if (nextStockOnHand is not null && nextStockOnHand < 0) throw new ArgumentException("StockOnHandBaseQty inválido.");
        if (nextStockBaseUnit is null && nextStockOnHand is not null)
            throw new ArgumentException("StockBaseUnit é obrigatório quando StockOnHandBaseQty for informado.");

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
            StockBaseUnit: nextStockBaseUnit,
            StockOnHandBaseQty: nextStockOnHand,
            IsActive: command.IsActive,
            CreatedAt: current.CreatedAt,
            UpdatedAt: DateTimeOffset.UtcNow
        );

        await _skus.UpdateAsync(updated, ct);
        return new SkuResult(
            Id: updated.Id,
            TenantId: updated.TenantId,
            CategoryCode: updated.CategoryCode,
            Code: updated.Code,
            Name: updated.Name,
            PriceCents: updated.PriceCents,
            AveragePrepSeconds: updated.AveragePrepSeconds,
            ImageUrl: updated.ImageUrl,
            NfeCProd: updated.NfeCProd,
            NfeCEan: updated.NfeCEan,
            NfeCfop: updated.NfeCfop,
            NfeUCom: updated.NfeUCom,
            NfeQCom: updated.NfeQCom,
            NfeVUnCom: updated.NfeVUnCom,
            NfeVProd: updated.NfeVProd,
            NfeCEanTrib: updated.NfeCEanTrib,
            NfeUTrib: updated.NfeUTrib,
            NfeQTrib: updated.NfeQTrib,
            NfeVUnTrib: updated.NfeVUnTrib,
            NfeIcmsOrig: updated.NfeIcmsOrig,
            NfeIcmsCst: updated.NfeIcmsCst,
            NfeIcmsModBc: updated.NfeIcmsModBc,
            NfeIcmsVBc: updated.NfeIcmsVBc,
            NfeIcmsPIcms: updated.NfeIcmsPIcms,
            NfeIcmsVIcms: updated.NfeIcmsVIcms,
            NfePisCst: updated.NfePisCst,
            NfePisVBc: updated.NfePisVBc,
            NfePisPPis: updated.NfePisPPis,
            NfePisVPis: updated.NfePisVPis,
            NfeCofinsCst: updated.NfeCofinsCst,
            NfeCofinsVBc: updated.NfeCofinsVBc,
            NfeCofinsPCofins: updated.NfeCofinsPCofins,
            NfeCofinsVCofins: updated.NfeCofinsVCofins,
            StockBaseUnit: updated.StockBaseUnit,
            StockOnHandBaseQty: updated.StockOnHandBaseQty,
            IsActive: updated.IsActive
        );
    }
}
