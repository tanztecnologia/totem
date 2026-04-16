using Microsoft.EntityFrameworkCore;
using TotemAPI.Features.Catalog.Application.Abstractions;
using TotemAPI.Features.Catalog.Domain;

namespace TotemAPI.Infrastructure.Persistence;

public sealed class EfCategoryRepository : ICategoryRepository
{
    public EfCategoryRepository(TotemDbContext db)
    {
        _db = db;
    }

    private readonly TotemDbContext _db;

    public async Task<IReadOnlyList<Category>> ListAsync(Guid tenantId, CancellationToken ct)
    {
        var rows = await _db.Categories.AsNoTracking().Where(x => x.TenantId == tenantId).OrderBy(x => x.Code).ToListAsync(ct);
        return rows.Select(x => x.ToDomain()).ToList().AsReadOnly();
    }

    public async Task<Category?> GetByCodeAsync(Guid tenantId, string code, CancellationToken ct)
    {
        var normalized = CategoryMapping.NormalizeNumericCode(code);
        if (normalized.Length == 0) return null;
        var row = await _db.Categories.AsNoTracking().SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Code == normalized, ct);
        return row?.ToDomain();
    }

    public async Task<Category?> GetBySlugAsync(Guid tenantId, string slug, CancellationToken ct)
    {
        var normalized = CategoryMapping.NormalizeSlug(slug);
        if (normalized.Length == 0) return null;
        var row = await _db.Categories.AsNoTracking().SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Slug == normalized, ct);
        return row?.ToDomain();
    }

    public async Task<int> GetMaxCodeNumberAsync(Guid tenantId, CancellationToken ct)
    {
        var conn = _db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
            await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        var provider = _db.Database.ProviderName ?? string.Empty;
        var isMySql = provider.Contains("MySql", StringComparison.OrdinalIgnoreCase);

        cmd.CommandText = isMySql
            ? "SELECT COALESCE(MAX(CAST(Code AS UNSIGNED)), 0) FROM categories WHERE TenantId = @tenantId AND Code REGEXP '^[0-9]+$'"
            : "SELECT COALESCE(MAX(CAST(Code AS INTEGER)), 0) FROM categories WHERE TenantId = $tenantId";
        var p = cmd.CreateParameter();
        p.ParameterName = isMySql ? "@tenantId" : "$tenantId";
        p.Value = tenantId.ToString("D");
        cmd.Parameters.Add(p);

        var scalar = await cmd.ExecuteScalarAsync(ct);
        if (scalar is null || scalar is DBNull) return 0;
        if (scalar is long l) return (int)l;
        if (scalar is int i) return i;
        if (int.TryParse(scalar.ToString(), out var parsed)) return parsed;
        return 0;
    }

    public async Task AddAsync(Category category, CancellationToken ct)
    {
        var row = new CategoryRow
        {
            Id = category.Id,
            TenantId = category.TenantId,
            Code = CategoryMapping.NormalizeNumericCode(category.Code),
            Slug = CategoryMapping.NormalizeSlug(category.Slug),
            Name = category.Name,
            IsActive = category.IsActive,
            CreatedAt = category.CreatedAt,
            UpdatedAt = category.UpdatedAt,
        };
        _db.Categories.Add(row);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Category category, CancellationToken ct)
    {
        var normalized = CategoryMapping.NormalizeNumericCode(category.Code);
        var row = await _db.Categories.SingleOrDefaultAsync(x => x.TenantId == category.TenantId && x.Code == normalized, ct);
        if (row is null) throw new InvalidOperationException("Categoria não encontrada.");

        row.Name = category.Name;
        row.IsActive = category.IsActive;
        row.Slug = CategoryMapping.NormalizeSlug(category.Slug);
        row.UpdatedAt = category.UpdatedAt;
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteByCodeAsync(Guid tenantId, string code, CancellationToken ct)
    {
        var normalized = CategoryMapping.NormalizeNumericCode(code);
        if (normalized.Length == 0) return;

        var row = await _db.Categories.SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Code == normalized, ct);
        if (row is null) return;
        _db.Categories.Remove(row);
        await _db.SaveChangesAsync(ct);
    }
}
