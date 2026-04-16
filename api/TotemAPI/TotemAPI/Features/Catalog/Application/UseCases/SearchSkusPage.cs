using TotemAPI.Features.Catalog.Application.Abstractions;

namespace TotemAPI.Features.Catalog.Application.UseCases;

public sealed record SearchSkusPageQuery(
    Guid TenantId,
    string? Query,
    int Limit,
    string? CursorCode,
    Guid? CursorId,
    bool IncludeInactive
);

public sealed record SkuSearchPageResult(
    IReadOnlyList<SkuResult> Items,
    string? NextCursorCode,
    Guid? NextCursorId
);

public sealed class SearchSkusPage
{
    public SearchSkusPage(ISkuRepository skus)
    {
        _skus = skus;
    }

    private readonly ISkuRepository _skus;

    public async Task<SkuSearchPageResult> HandleAsync(SearchSkusPageQuery query, CancellationToken ct)
    {
        if (query.TenantId == Guid.Empty) throw new ArgumentException("TenantId inválido.");
        if (query.Limit <= 0) throw new ArgumentException("Limit inválido.");

        var page = await _skus.SearchPageAsync(
            tenantId: query.TenantId,
            query: query.Query,
            limit: query.Limit,
            cursorCode: query.CursorCode,
            cursorId: query.CursorId,
            includeInactive: query.IncludeInactive,
            ct: ct
        );

        var items = page.Items
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
                        StockBaseUnit: s.StockBaseUnit,
                        StockOnHandBaseQty: s.StockOnHandBaseQty,
                        IsActive: s.IsActive
                    )
            )
            .ToList()
            .AsReadOnly();

        return new SkuSearchPageResult(items, page.NextCursorCode, page.NextCursorId);
    }
}
