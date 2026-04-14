using TotemAPI.Features.Catalog.Domain;

namespace TotemAPI.Features.Catalog.Application.Abstractions;

public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> ListAsync(Guid tenantId, CancellationToken ct);
    Task<Category?> GetByCodeAsync(Guid tenantId, string code, CancellationToken ct);
    Task<Category?> GetBySlugAsync(Guid tenantId, string slug, CancellationToken ct);
    Task<int> GetMaxCodeNumberAsync(Guid tenantId, CancellationToken ct);
    Task AddAsync(Category category, CancellationToken ct);
    Task UpdateAsync(Category category, CancellationToken ct);
    Task DeleteByCodeAsync(Guid tenantId, string code, CancellationToken ct);
}
