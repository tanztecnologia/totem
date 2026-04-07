using System.Collections.Concurrent;
using TotemAPI.Features.Identity.Application.Abstractions;
using TotemAPI.Features.Identity.Domain;

namespace TotemAPI.Features.Identity.Infrastructure;

public sealed class InMemoryTenantRepository : ITenantRepository
{
    private readonly ConcurrentDictionary<Guid, Tenant> _byId = new();
    private readonly ConcurrentDictionary<string, Guid> _idByName = new(StringComparer.OrdinalIgnoreCase);

    public Task<Tenant?> GetByNameAsync(string name, CancellationToken ct)
    {
        var key = (name ?? string.Empty).Trim();
        if (key.Length == 0) return Task.FromResult<Tenant?>(null);

        if (!_idByName.TryGetValue(key, out var id)) return Task.FromResult<Tenant?>(null);
        return Task.FromResult(_byId.TryGetValue(id, out var tenant) ? tenant : null);
    }

    public Task<Tenant?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return Task.FromResult(_byId.TryGetValue(id, out var tenant) ? tenant : null);
    }

    public Task AddAsync(Tenant tenant, CancellationToken ct)
    {
        if (!_byId.TryAdd(tenant.Id, tenant)) throw new InvalidOperationException("Tenant já existe.");
        if (!_idByName.TryAdd(tenant.Name.Trim(), tenant.Id)) throw new InvalidOperationException("Tenant já existe.");
        return Task.CompletedTask;
    }
}

