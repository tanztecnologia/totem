using TotemAPI.Features.Catalog.Domain;

namespace TotemAPI.Features.Catalog.Application.Abstractions;

public interface ISkuRepository
{
    Task<IReadOnlyList<Sku>> ListAsync(Guid tenantId, CancellationToken ct);
    Task<Sku?> GetByIdAsync(Guid tenantId, Guid skuId, CancellationToken ct);
    Task<Sku?> GetByCodeAsync(Guid tenantId, string code, CancellationToken ct);
    Task AddAsync(Sku sku, CancellationToken ct);
    Task UpdateAsync(Sku sku, CancellationToken ct);
    Task DeleteAsync(Guid tenantId, Guid skuId, CancellationToken ct);
}

