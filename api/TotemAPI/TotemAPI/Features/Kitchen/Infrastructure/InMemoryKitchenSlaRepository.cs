using System.Collections.Concurrent;
using TotemAPI.Features.Kitchen.Application.Abstractions;
using TotemAPI.Features.Kitchen.Domain;

namespace TotemAPI.Features.Kitchen.Infrastructure;

public sealed class InMemoryKitchenSlaRepository : IKitchenSlaRepository
{
    private readonly ConcurrentDictionary<Guid, KitchenSla> _byTenant = new();

    public Task<KitchenSla?> GetAsync(Guid tenantId, CancellationToken ct)
    {
        return Task.FromResult(_byTenant.TryGetValue(tenantId, out var sla) ? sla : null);
    }

    public Task UpsertAsync(KitchenSla sla, CancellationToken ct)
    {
        _byTenant[sla.TenantId] = sla;
        return Task.CompletedTask;
    }
}
