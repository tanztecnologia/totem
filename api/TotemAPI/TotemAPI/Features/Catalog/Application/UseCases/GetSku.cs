using TotemAPI.Features.Catalog.Application.Abstractions;

namespace TotemAPI.Features.Catalog.Application.UseCases;

public sealed record GetSkuQuery(Guid TenantId, Guid SkuId);

public sealed class GetSku
{
    public GetSku(ISkuRepository skus)
    {
        _skus = skus;
    }

    private readonly ISkuRepository _skus;

    public async Task<SkuResult?> HandleAsync(GetSkuQuery query, CancellationToken ct)
    {
        if (query.TenantId == Guid.Empty) throw new ArgumentException("TenantId inválido.");
        if (query.SkuId == Guid.Empty) throw new ArgumentException("SkuId inválido.");

        var sku = await _skus.GetByIdAsync(query.TenantId, query.SkuId, ct);
        if (sku is null) return null;
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
            TracksStock: sku.TracksStock,
            StockBaseUnit: sku.StockBaseUnit,
            StockOnHandBaseQty: sku.StockOnHandBaseQty,
            IsActive: sku.IsActive
        );
    }
}
