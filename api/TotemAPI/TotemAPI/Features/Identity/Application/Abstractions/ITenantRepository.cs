using TotemAPI.Features.Identity.Domain;

namespace TotemAPI.Features.Identity.Application.Abstractions;

public interface ITenantRepository
{
    Task<Tenant?> GetByNameAsync(string name, CancellationToken ct);
    Task<Tenant?> GetByIdAsync(Guid id, CancellationToken ct);
    Task AddAsync(Tenant tenant, CancellationToken ct);
}

