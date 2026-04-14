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

    public Task<int> GetMaxCodeNumberAsync(Guid tenantId, CancellationToken ct)
    {
        var max = _byId.Values
            .Where(x => x.TenantId == tenantId)
            .Select(x => int.TryParse(x.Code, out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();
        return Task.FromResult(max);
    }

    public Task<SkuSearchPageSnapshot> SearchPageAsync(
        Guid tenantId,
        string? query,
        int limit,
        string? cursorCode,
        Guid? cursorId,
        bool includeInactive,
        CancellationToken ct
    )
    {
        if (limit <= 0) return Task.FromResult(new SkuSearchPageSnapshot(Array.Empty<Sku>(), null, null));
        if (limit > 200) limit = 200;

        var trimmedQuery = (query ?? string.Empty).Trim();
        var normalizedQuery = NormalizeCode(trimmedQuery);

        var normalizedCursorCode = NormalizeCode(cursorCode ?? string.Empty);
        var hasCursor = normalizedCursorCode.Length > 0 && cursorId is not null && cursorId != Guid.Empty;

        var list = _byId.Values
            .Where(s => s.TenantId == tenantId)
            .Where(s => includeInactive || s.IsActive)
            .Where(
                s =>
                    normalizedQuery.Length == 0
                    || NormalizeCode(s.Code).Contains(normalizedQuery, StringComparison.Ordinal)
                    || s.Name.Contains(trimmedQuery, StringComparison.OrdinalIgnoreCase)
            )
            .OrderBy(s => NormalizeCode(s.Code), StringComparer.Ordinal)
            .ThenBy(s => s.Id.ToString(), StringComparer.Ordinal)
            .ToList();

        if (hasCursor)
        {
            var cursorIdStr = cursorId!.Value.ToString();
            list = list
                .Where(
                    s =>
                    {
                        var c = NormalizeCode(s.Code);
                        var id = s.Id.ToString();
                        return string.CompareOrdinal(c, normalizedCursorCode) > 0
                            || (string.Equals(c, normalizedCursorCode, StringComparison.Ordinal) && string.CompareOrdinal(id, cursorIdStr) > 0);
                    }
                )
                .ToList();
        }

        string? nextCursorCode = null;
        Guid? nextCursorId = null;

        if (list.Count > limit)
        {
            var last = list[limit - 1];
            nextCursorCode = last.Code;
            nextCursorId = last.Id;
            list = list.Take(limit).ToList();
        }

        return Task.FromResult(new SkuSearchPageSnapshot(list.AsReadOnly(), nextCursorCode, nextCursorId));
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
