using System.Collections.Concurrent;
using TotemAPI.Features.Catalog.Application.Abstractions;
using TotemAPI.Features.Catalog.Domain;

namespace TotemAPI.Features.Catalog.Infrastructure;

public sealed class InMemorySkuRepository : ISkuRepository
{
    private readonly ConcurrentDictionary<Guid, Sku> _byId = new();
    private readonly ConcurrentDictionary<(Guid TenantId, string Code), Guid> _idByCode = new();
    private readonly ConcurrentDictionary<(Guid TenantId, Guid SkuId), ConcurrentDictionary<Guid, SkuStockConsumption>> _stockConsumptions = new();
    private readonly ConcurrentDictionary<(Guid TenantId, Guid SkuId), ConcurrentDictionary<Guid, SkuImage>> _images = new();
    private readonly List<SkuStockLedgerEntry> _stockLedger = new();

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

    public Task<IReadOnlyList<SkuStockConsumption>> ListStockConsumptionsAsync(Guid tenantId, Guid skuId, CancellationToken ct)
    {
        if (tenantId == Guid.Empty) return Task.FromResult<IReadOnlyList<SkuStockConsumption>>(Array.Empty<SkuStockConsumption>());
        if (skuId == Guid.Empty) return Task.FromResult<IReadOnlyList<SkuStockConsumption>>(Array.Empty<SkuStockConsumption>());

        if (!_stockConsumptions.TryGetValue((tenantId, skuId), out var map))
            return Task.FromResult<IReadOnlyList<SkuStockConsumption>>(Array.Empty<SkuStockConsumption>());

        var list = map.Values.OrderBy(x => x.SourceSkuId).ToList().AsReadOnly();
        return Task.FromResult<IReadOnlyList<SkuStockConsumption>>(list);
    }

    public Task ReplaceStockConsumptionsAsync(Guid tenantId, Guid skuId, IReadOnlyList<SkuStockConsumption> items, CancellationToken ct)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId inválido.");
        if (skuId == Guid.Empty) throw new ArgumentException("SkuId inválido.");

        var next = new ConcurrentDictionary<Guid, SkuStockConsumption>();
        foreach (var item in items)
        {
            if (item.TenantId != tenantId) throw new ArgumentException("TenantId inválido.");
            if (item.SkuId != skuId) throw new ArgumentException("SkuId inválido.");
            if (item.SourceSkuId == Guid.Empty) throw new ArgumentException("SourceSkuId inválido.");
            if (item.QuantityBase <= 0) throw new ArgumentException("QuantityBase inválido.");

            var stored = item with { Id = item.Id == Guid.Empty ? Guid.NewGuid() : item.Id };
            if (!next.TryAdd(stored.SourceSkuId, stored)) throw new InvalidOperationException("Consumo duplicado.");
        }

        _stockConsumptions[(tenantId, skuId)] = next;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<SkuImage>> ListImagesAsync(Guid tenantId, Guid skuId, CancellationToken ct)
    {
        if (!_images.TryGetValue((tenantId, skuId), out var map))
            return Task.FromResult<IReadOnlyList<SkuImage>>(Array.Empty<SkuImage>());

        var list = map.Values
            .OrderBy(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .ToList()
            .AsReadOnly();

        return Task.FromResult<IReadOnlyList<SkuImage>>(list);
    }

    public Task<int> CountImagesAsync(Guid tenantId, Guid skuId, CancellationToken ct)
    {
        if (!_images.TryGetValue((tenantId, skuId), out var map))
            return Task.FromResult(0);
        return Task.FromResult(map.Count);
    }

    public Task AddImageAsync(SkuImage image, CancellationToken ct)
    {
        var map = _images.GetOrAdd((image.TenantId, image.SkuId), _ => new ConcurrentDictionary<Guid, SkuImage>());
        if (!map.TryAdd(image.Id, image)) throw new InvalidOperationException("Imagem já cadastrada.");
        return Task.CompletedTask;
    }

    public Task<SkuImage?> DeleteImageAsync(Guid tenantId, Guid skuId, Guid imageId, CancellationToken ct)
    {
        if (!_images.TryGetValue((tenantId, skuId), out var map))
            return Task.FromResult<SkuImage?>(null);
        if (!map.TryRemove(imageId, out var removed))
            return Task.FromResult<SkuImage?>(null);
        return Task.FromResult<SkuImage?>(removed);
    }

    public Task<SkuStockLedgerEntry> AddStockLedgerEntryAsync(SkuStockLedgerEntry entry, CancellationToken ct)
    {
        if (entry.TenantId == Guid.Empty) throw new ArgumentException("TenantId inválido.");
        if (entry.SkuId == Guid.Empty) throw new ArgumentException("SkuId inválido.");
        if (entry.DeltaBaseQty == 0) throw new ArgumentException("DeltaBaseQty não pode ser zero.");

        if (!_byId.TryGetValue(entry.SkuId, out var sku) || sku.TenantId != entry.TenantId)
            throw new InvalidOperationException("SKU não encontrado.");

        if (!sku.TracksStock) throw new InvalidOperationException("SKU não controla estoque próprio.");
        if (sku.StockBaseUnit is null || sku.StockOnHandBaseQty is null)
            throw new InvalidOperationException("Controle de estoque não configurado para o SKU.");

        var stockAfter = sku.StockOnHandBaseQty.Value + entry.DeltaBaseQty;
        if (stockAfter < 0) throw new InvalidOperationException("Estoque insuficiente.");

        var persisted = entry with
        {
            Id = entry.Id == Guid.Empty ? Guid.NewGuid() : entry.Id,
            StockAfterBaseQty = stockAfter,
            CreatedAt = entry.CreatedAt == default ? DateTimeOffset.UtcNow : entry.CreatedAt,
        };

        _stockLedger.Add(persisted);
        _byId[entry.SkuId] = sku with { StockOnHandBaseQty = stockAfter, UpdatedAt = persisted.CreatedAt };

        return Task.FromResult(persisted);
    }

    public Task<IReadOnlyList<SkuStockLedgerEntry>> ListStockLedgerAsync(
        Guid tenantId,
        Guid skuId,
        int limit,
        DateTimeOffset? cursorCreatedAt,
        Guid? cursorId,
        CancellationToken ct
    )
    {
        if (limit <= 0) return Task.FromResult<IReadOnlyList<SkuStockLedgerEntry>>(Array.Empty<SkuStockLedgerEntry>());
        if (limit > 500) limit = 500;

        var entries = _stockLedger
            .Where(x => x.TenantId == tenantId && x.SkuId == skuId);

        if (cursorCreatedAt is not null && cursorId is not null && cursorId != Guid.Empty)
        {
            entries = entries.Where(x =>
                x.CreatedAt < cursorCreatedAt ||
                (x.CreatedAt == cursorCreatedAt && x.Id.CompareTo(cursorId.Value) < 0));
        }

        var result = entries
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Take(limit)
            .ToList()
            .AsReadOnly();

        return Task.FromResult<IReadOnlyList<SkuStockLedgerEntry>>(result);
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
        _stockConsumptions.TryRemove((tenantId, skuId), out _);
        _images.TryRemove((tenantId, skuId), out _);
        return Task.CompletedTask;
    }

    private static string NormalizeCode(string code)
    {
        return (code ?? string.Empty).Trim().ToUpperInvariant();
    }
}
