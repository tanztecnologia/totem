using TotemAPI.Features.Catalog.Application.Abstractions;

namespace TotemAPI.Features.Catalog.Application.UseCases;

public sealed record ListSkusQuery(Guid TenantId);

public sealed class ListSkus
{
    public ListSkus(ISkuRepository skus)
    {
        _skus = skus;
    }

    private readonly ISkuRepository _skus;

    public async Task<IReadOnlyList<SkuResult>> HandleAsync(ListSkusQuery query, CancellationToken ct)
    {
        if (query.TenantId == Guid.Empty) throw new ArgumentException("TenantId inválido.");

        var list = await _skus.ListAsync(query.TenantId, ct);
        return list
            .Select(
                s =>
                    new SkuResult(
                        Id: s.Id,
                        TenantId: s.TenantId,
                        CategoryCode: s.CategoryCode,
                        Code: s.Code,
                        Name: s.Name,
                        PriceCents: s.PriceCents,
                        AveragePrepSeconds: s.AveragePrepSeconds,
                        ImageUrl: s.ImageUrl,
                        NfeCProd: s.NfeCProd,
                        NfeCEan: s.NfeCEan,
                        NfeCfop: s.NfeCfop,
                        NfeUCom: s.NfeUCom,
                        NfeQCom: s.NfeQCom,
                        NfeVUnCom: s.NfeVUnCom,
                        NfeVProd: s.NfeVProd,
                        NfeCEanTrib: s.NfeCEanTrib,
                        NfeUTrib: s.NfeUTrib,
                        NfeQTrib: s.NfeQTrib,
                        NfeVUnTrib: s.NfeVUnTrib,
                        NfeIcmsOrig: s.NfeIcmsOrig,
                        NfeIcmsCst: s.NfeIcmsCst,
                        NfeIcmsModBc: s.NfeIcmsModBc,
                        NfeIcmsVBc: s.NfeIcmsVBc,
                        NfeIcmsPIcms: s.NfeIcmsPIcms,
                        NfeIcmsVIcms: s.NfeIcmsVIcms,
                        NfePisCst: s.NfePisCst,
                        NfePisVBc: s.NfePisVBc,
                        NfePisPPis: s.NfePisPPis,
                        NfePisVPis: s.NfePisVPis,
                        NfeCofinsCst: s.NfeCofinsCst,
                        NfeCofinsVBc: s.NfeCofinsVBc,
                        NfeCofinsPCofins: s.NfeCofinsPCofins,
                        NfeCofinsVCofins: s.NfeCofinsVCofins,
                        TracksStock: s.TracksStock,
                        StockBaseUnit: s.StockBaseUnit,
                        StockOnHandBaseQty: s.StockOnHandBaseQty,
                        IsActive: s.IsActive
                    )
            )
            .ToList();
    }
}
