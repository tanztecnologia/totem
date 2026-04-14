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
            .Select(s => new SkuResult(s.Id, s.TenantId, s.CategoryCode, s.Code, s.Name, s.PriceCents, s.AveragePrepSeconds, s.ImageUrl, s.IsActive))
            .ToList()
            .AsReadOnly();

        return new SkuSearchPageResult(items, page.NextCursorCode, page.NextCursorId);
    }
}
