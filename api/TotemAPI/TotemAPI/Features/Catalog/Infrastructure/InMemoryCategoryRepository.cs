using System.Collections.Concurrent;
using TotemAPI.Features.Catalog.Application.Abstractions;
using TotemAPI.Features.Catalog.Domain;
using TotemAPI.Infrastructure.Persistence;

namespace TotemAPI.Features.Catalog.Infrastructure;

public sealed class InMemoryCategoryRepository : ICategoryRepository
{
    private readonly ConcurrentDictionary<Guid, Category> _byId = new();
    private readonly ConcurrentDictionary<(Guid TenantId, string Code), Guid> _idByCode = new();
    private readonly ConcurrentDictionary<(Guid TenantId, string Slug), Guid> _idBySlug = new();

    public Task<IReadOnlyList<Category>> ListAsync(Guid tenantId, CancellationToken ct)
    {
        var list = _byId.Values.Where(x => x.TenantId == tenantId).OrderBy(x => x.Code).ToList().AsReadOnly();
        return Task.FromResult<IReadOnlyList<Category>>(list);
    }

    public Task<Category?> GetByCodeAsync(Guid tenantId, string code, CancellationToken ct)
    {
        var key = (tenantId, CategoryMapping.NormalizeNumericCode(code));
        if (!_idByCode.TryGetValue(key, out var id)) return Task.FromResult<Category?>(null);
        return Task.FromResult(_byId.TryGetValue(id, out var c) ? c : null);
    }

    public Task<Category?> GetBySlugAsync(Guid tenantId, string slug, CancellationToken ct)
    {
        var key = (tenantId, CategoryMapping.NormalizeSlug(slug));
        if (!_idBySlug.TryGetValue(key, out var id)) return Task.FromResult<Category?>(null);
        return Task.FromResult(_byId.TryGetValue(id, out var c) ? c : null);
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

    public Task AddAsync(Category category, CancellationToken ct)
    {
        var normalizedCode = CategoryMapping.NormalizeNumericCode(category.Code);
        if (normalizedCode.Length == 0) throw new ArgumentException("Code inválido.");

        var normalizedSlug = CategoryMapping.NormalizeSlug(category.Slug);
        if (normalizedSlug.Length == 0) throw new ArgumentException("Slug inválido.");

        var stored = category with { Code = normalizedCode, Slug = normalizedSlug };
        if (!_byId.TryAdd(stored.Id, stored)) throw new InvalidOperationException("Categoria já existe.");
        if (!_idByCode.TryAdd((stored.TenantId, stored.Code), stored.Id))
        {
            _byId.TryRemove(stored.Id, out _);
            throw new InvalidOperationException("Code já está em uso.");
        }
        if (!_idBySlug.TryAdd((stored.TenantId, stored.Slug), stored.Id))
        {
            _byId.TryRemove(stored.Id, out _);
            _idByCode.TryRemove((stored.TenantId, stored.Code), out _);
            throw new InvalidOperationException("Slug já está em uso.");
        }
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Category category, CancellationToken ct)
    {
        var normalizedCode = CategoryMapping.NormalizeNumericCode(category.Code);
        if (normalizedCode.Length == 0) throw new ArgumentException("Code inválido.");

        var normalizedSlug = CategoryMapping.NormalizeSlug(category.Slug);
        if (normalizedSlug.Length == 0) throw new ArgumentException("Slug inválido.");

        var key = (category.TenantId, normalizedCode);
        if (!_idByCode.TryGetValue(key, out var id)) throw new InvalidOperationException("Categoria não encontrada.");

        var current = _byId[id];
        var currentSlugKey = (current.TenantId, current.Slug);
        var nextSlugKey = (category.TenantId, normalizedSlug);

        if (currentSlugKey != nextSlugKey)
        {
            if (_idBySlug.TryGetValue(nextSlugKey, out var otherId) && otherId != id)
                throw new InvalidOperationException("Slug já está em uso.");

            if (!_idBySlug.TryRemove(currentSlugKey, out _)) throw new InvalidOperationException("Falha ao atualizar categoria.");
            if (!_idBySlug.TryAdd(nextSlugKey, id))
            {
                _idBySlug.TryAdd(currentSlugKey, id);
                throw new InvalidOperationException("Slug já está em uso.");
            }
        }

        _byId[id] = category with { Id = id, Code = normalizedCode, Slug = normalizedSlug };
        return Task.CompletedTask;
    }

    public Task DeleteByCodeAsync(Guid tenantId, string code, CancellationToken ct)
    {
        var normalized = CategoryMapping.NormalizeNumericCode(code);
        if (normalized.Length == 0) return Task.CompletedTask;

        if (!_idByCode.TryRemove((tenantId, normalized), out var id)) return Task.CompletedTask;
        if (_byId.TryRemove(id, out var removed))
        {
            _idBySlug.TryRemove((tenantId, removed.Slug), out _);
        }
        return Task.CompletedTask;
    }
}
