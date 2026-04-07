using System.Collections.Concurrent;
using TotemAPI.Features.Catalog.Application.Abstractions;
using TotemAPI.Features.Catalog.Domain;

namespace TotemAPI.Features.Catalog.Infrastructure;

public sealed class InMemorySkuRepository : ISkuRepository
{
    private readonly ConcurrentDictionary<Guid, Sku> _byId = new();
    private readonly ConcurrentDictionary<(Guid TenantId, string Code), Guid> _idByCode = new();

    public Task<IReadOnlyList<Sku>> ListAsync(Guid tenantId, CancellationToken ct)
    {
        var list = _byId.Values
            .Where(s => s.TenantId == tenantId)
            .OrderBy(s => s.Name)
            .ToList()
            .AsReadOnly();
        return Task.FromResult<IReadOnlyList<Sku>>(list);
    }

    public Task<Sku?> GetByIdAsync(Guid tenantId, Guid skuId, CancellationToken ct)
    {
        if (!_byId.TryGetValue(skuId, out var sku)) return Task.FromResult<Sku?>(null);
        return Task.FromResult(sku.TenantId == tenantId ? sku : null);
    }

    public Task<Sku?> GetByCodeAsync(Guid tenantId, string code, CancellationToken ct)
    {
        var key = (tenantId, NormalizeCode(code));
        if (!_idByCode.TryGetValue(key, out var id)) return Task.FromResult<Sku?>(null);
        return Task.FromResult(_byId.TryGetValue(id, out var sku) ? sku : null);
    }

    public Task AddAsync(Sku sku, CancellationToken ct)
    {
        if (!_byId.TryAdd(sku.Id, sku)) throw new InvalidOperationException("SKU já existe.");

        var codeKey = NormalizeCode(sku.Code);
        if (!_idByCode.TryAdd((sku.TenantId, codeKey), sku.Id))
        {
            _byId.TryRemove(sku.Id, out _);
            throw new InvalidOperationException("Code já está em uso.");
        }

        return Task.CompletedTask;
    }

    public Task UpdateAsync(Sku sku, CancellationToken ct)
    {
        if (!_byId.TryGetValue(sku.Id, out var current)) throw new InvalidOperationException("SKU não encontrado.");
        if (current.TenantId != sku.TenantId) throw new InvalidOperationException("SKU inválido.");

        var currentKey = (sku.TenantId, NormalizeCode(current.Code));
        var nextKey = (sku.TenantId, NormalizeCode(sku.Code));

        if (currentKey != nextKey)
        {
            if (_idByCode.TryGetValue(nextKey, out var otherId) && otherId != sku.Id)
                throw new InvalidOperationException("Code já está em uso.");

            if (!_idByCode.TryRemove(currentKey, out _)) throw new InvalidOperationException("Falha ao atualizar SKU.");
            if (!_idByCode.TryAdd(nextKey, sku.Id))
            {
                _idByCode.TryAdd(currentKey, sku.Id);
                throw new InvalidOperationException("Code já está em uso.");
            }
        }

        _byId[sku.Id] = sku;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid tenantId, Guid skuId, CancellationToken ct)
    {
        if (!_byId.TryGetValue(skuId, out var sku)) return Task.CompletedTask;
        if (sku.TenantId != tenantId) return Task.CompletedTask;

        _byId.TryRemove(skuId, out _);
        _idByCode.TryRemove((tenantId, NormalizeCode(sku.Code)), out _);
        return Task.CompletedTask;
    }

    private static string NormalizeCode(string code)
    {
        return (code ?? string.Empty).Trim().ToUpperInvariant();
    }
}

