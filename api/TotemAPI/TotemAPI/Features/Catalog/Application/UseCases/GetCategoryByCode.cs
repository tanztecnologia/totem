using TotemAPI.Features.Catalog.Application.Abstractions;
using TotemAPI.Infrastructure.Persistence;

namespace TotemAPI.Features.Catalog.Application.UseCases;

public sealed record GetCategoryByCodeQuery(Guid TenantId, string Code);

public sealed class GetCategoryByCode
{
    public GetCategoryByCode(ICategoryRepository categories)
    {
        _categories = categories;
    }

    private readonly ICategoryRepository _categories;

    public async Task<CategoryResult?> HandleAsync(GetCategoryByCodeQuery query, CancellationToken ct)
    {
        if (query.TenantId == Guid.Empty) throw new ArgumentException("TenantId inválido.");
        var normalizedCode = CategoryMapping.NormalizeNumericCode(query.Code);
        if (normalizedCode.Length == 0) throw new ArgumentException("Code inválido.");

        var category = await _categories.GetByCodeAsync(query.TenantId, normalizedCode, ct);
        if (category is null) return null;
        return new CategoryResult(category.Id, category.TenantId, category.Code, category.Slug, category.Name, category.IsActive);
    }
}
