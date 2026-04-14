using TotemAPI.Features.Catalog.Application.Abstractions;

namespace TotemAPI.Features.Catalog.Application.UseCases;

public sealed record ListCategoriesQuery(Guid TenantId, bool IncludeInactive);

public sealed class ListCategories
{
    public ListCategories(ICategoryRepository categories)
    {
        _categories = categories;
    }

    private readonly ICategoryRepository _categories;

    public async Task<IReadOnlyList<CategoryResult>> HandleAsync(ListCategoriesQuery query, CancellationToken ct)
    {
        if (query.TenantId == Guid.Empty) throw new ArgumentException("TenantId inválido.");

        var list = await _categories.ListAsync(query.TenantId, ct);
        if (!query.IncludeInactive) list = list.Where(x => x.IsActive).ToList().AsReadOnly();

        return list.Select(x => new CategoryResult(x.Id, x.TenantId, x.Code, x.Slug, x.Name, x.IsActive)).ToList().AsReadOnly();
    }
}
