using TotemAPI.Features.Catalog.Domain;

namespace TotemAPI.Features.Catalog.Application.Abstractions;

public interface ISkuRepository
{
    Task<IReadOnlyList<Sku>> ListAsync(Guid tenantId, CancellationToken ct);
    Task<Sku?> GetByIdAsync(Guid tenantId, Guid skuId, CancellationToken ct);
    Task<Sku?> GetByCodeAsync(Guid tenantId, string code, CancellationToken ct);
    Task<int> GetMaxCodeNumberAsync(Guid tenantId, CancellationToken ct);
    Task<SkuSearchPageSnapshot> SearchPageAsync(
        Guid tenantId,
        string? query,
        int limit,
        string? cursorCode,
        Guid? cursorId,
        bool includeInactive,
        CancellationToken ct
    );
    Task AddAsync(Sku sku, CancellationToken ct);
    Task UpdateAsync(Sku sku, CancellationToken ct);
    Task DeleteAsync(Guid tenantId, Guid skuId, CancellationToken ct);
}

public sealed record SkuSearchPageSnapshot(
    IReadOnlyList<Sku> Items,
    string? NextCursorCode,
    Guid? NextCursorId
);
