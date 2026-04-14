using Microsoft.EntityFrameworkCore;
using TotemAPI.Features.Catalog.Application.Abstractions;
using TotemAPI.Features.Catalog.Domain;

namespace TotemAPI.Infrastructure.Persistence;

public sealed class EfSkuRepository : ISkuRepository
{
    public EfSkuRepository(TotemDbContext db)
    {
        _db = db;
    }

    private readonly TotemDbContext _db;

    public async Task<IReadOnlyList<Sku>> ListAsync(Guid tenantId, CancellationToken ct)
    {
        return await _db.Skus
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .OrderBy(x => x.Name)
            .Select(x => x.ToDomain())
            .ToListAsync(ct);
    }

    public async Task<Sku?> GetByIdAsync(Guid tenantId, Guid skuId, CancellationToken ct)
    {
        var row = await _db.Skus.AsNoTracking().SingleOrDefaultAsync(x => x.Id == skuId && x.TenantId == tenantId, ct);
        return row?.ToDomain();
    }

    public async Task<Sku?> GetByCodeAsync(Guid tenantId, string code, CancellationToken ct)
    {
        var normalized = SkuMapping.NormalizeCode(code);
        if (normalized.Length == 0) return null;

        var row = await _db.Skus.AsNoTracking().SingleOrDefaultAsync(x => x.TenantId == tenantId && x.NormalizedCode == normalized, ct);
        return row?.ToDomain();
    }

    public async Task<int> GetMaxCodeNumberAsync(Guid tenantId, CancellationToken ct)
    {
        var conn = _db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
            await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COALESCE(MAX(CAST(Code AS INTEGER)), 0) FROM skus WHERE TenantId = $tenantId";
        var p = cmd.CreateParameter();
        p.ParameterName = "$tenantId";
        p.Value = tenantId.ToString();
        cmd.Parameters.Add(p);

        var scalar = await cmd.ExecuteScalarAsync(ct);
        if (scalar is null || scalar is DBNull) return 0;
        if (scalar is long l) return (int)l;
        if (scalar is int i) return i;
        if (int.TryParse(scalar.ToString(), out var parsed)) return parsed;
        return 0;
    }

    public async Task<SkuSearchPageSnapshot> SearchPageAsync(
        Guid tenantId,
        string? query,
        int limit,
        string? cursorCode,
        Guid? cursorId,
        bool includeInactive,
        CancellationToken ct
    )
    {
        if (limit <= 0) return new SkuSearchPageSnapshot(Array.Empty<Sku>(), null, null);
        if (limit > 200) limit = 200;

        var trimmedQuery = (query ?? string.Empty).Trim();
        var normalizedQuery = SkuMapping.NormalizeCode(trimmedQuery);

        var trimmedCursorCode = (cursorCode ?? string.Empty).Trim();
        var normalizedCursorCode = SkuMapping.NormalizeCode(trimmedCursorCode);
        var hasCursor = normalizedCursorCode.Length > 0 && cursorId is not null && cursorId != Guid.Empty;

        var sql = "SELECT * FROM skus WHERE TenantId = {0}";
        var args = new List<object> { tenantId };
        var idx = 1;

        if (!includeInactive)
        {
            sql += $" AND IsActive = {{{idx}}}";
            args.Add(true);
            idx++;
        }

        if (normalizedQuery.Length > 0)
        {
            sql += $" AND (NormalizedCode LIKE {{{idx}}} OR Name LIKE {{{idx + 1}}})";
            args.Add($"%{normalizedQuery}%");
            args.Add($"%{trimmedQuery}%");
            idx += 2;
        }

        if (hasCursor)
        {
            sql += $" AND (NormalizedCode > {{{idx}}} OR (NormalizedCode = {{{idx}}} AND Id > {{{idx + 1}}}))";
            args.Add(normalizedCursorCode);
            args.Add(cursorId!.Value);
            idx += 2;
        }

        sql += $" ORDER BY NormalizedCode ASC, Id ASC LIMIT {{{idx}}}";
        args.Add(limit + 1);

        var rows = await _db.Skus.FromSqlRaw(sql, args.ToArray()).AsNoTracking().ToListAsync(ct);

        string? nextCursorCode = null;
        Guid? nextCursorId = null;

        if (rows.Count > limit)
        {
            var last = rows[limit - 1];
            nextCursorCode = last.Code;
            nextCursorId = last.Id;
            rows = rows.Take(limit).ToList();
        }

        var items = rows.Select(x => x.ToDomain()).ToList().AsReadOnly();
        return new SkuSearchPageSnapshot(items, nextCursorCode, nextCursorId);
    }

    public async Task AddAsync(Sku sku, CancellationToken ct)
    {
        var row = new SkuRow
        {
            Id = sku.Id,
            TenantId = sku.TenantId,
            CategoryCode = CategoryMapping.NormalizeNumericCode(sku.CategoryCode),
            Code = sku.Code,
            NormalizedCode = SkuMapping.NormalizeCode(sku.Code),
            Name = sku.Name,
            PriceCents = sku.PriceCents,
            AveragePrepSeconds = sku.AveragePrepSeconds,
            ImageUrl = sku.ImageUrl,
            NfeCProd = sku.NfeCProd,
            NfeCEan = sku.NfeCEan,
            NfeCfop = sku.NfeCfop,
            NfeUCom = sku.NfeUCom,
            NfeQCom = sku.NfeQCom,
            NfeVUnCom = sku.NfeVUnCom,
            NfeVProd = sku.NfeVProd,
            NfeCEanTrib = sku.NfeCEanTrib,
            NfeUTrib = sku.NfeUTrib,
            NfeQTrib = sku.NfeQTrib,
            NfeVUnTrib = sku.NfeVUnTrib,
            IsActive = sku.IsActive,
            CreatedAt = sku.CreatedAt,
            UpdatedAt = sku.UpdatedAt,
        };

        _db.Skus.Add(row);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Sku sku, CancellationToken ct)
    {
        var row = await _db.Skus.SingleOrDefaultAsync(x => x.Id == sku.Id && x.TenantId == sku.TenantId, ct);
        if (row is null) throw new InvalidOperationException("SKU não encontrado.");

        row.Code = sku.Code;
        row.CategoryCode = CategoryMapping.NormalizeNumericCode(sku.CategoryCode);
        row.NormalizedCode = SkuMapping.NormalizeCode(sku.Code);
        row.Name = sku.Name;
        row.PriceCents = sku.PriceCents;
        row.AveragePrepSeconds = sku.AveragePrepSeconds;
        row.ImageUrl = sku.ImageUrl;
        row.NfeCProd = sku.NfeCProd;
        row.NfeCEan = sku.NfeCEan;
        row.NfeCfop = sku.NfeCfop;
        row.NfeUCom = sku.NfeUCom;
        row.NfeQCom = sku.NfeQCom;
        row.NfeVUnCom = sku.NfeVUnCom;
        row.NfeVProd = sku.NfeVProd;
        row.NfeCEanTrib = sku.NfeCEanTrib;
        row.NfeUTrib = sku.NfeUTrib;
        row.NfeQTrib = sku.NfeQTrib;
        row.NfeVUnTrib = sku.NfeVUnTrib;
        row.IsActive = sku.IsActive;
        row.UpdatedAt = sku.UpdatedAt;

        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid tenantId, Guid skuId, CancellationToken ct)
    {
        var row = await _db.Skus.SingleOrDefaultAsync(x => x.Id == skuId && x.TenantId == tenantId, ct);
        if (row is null) return;
        _db.Skus.Remove(row);
        await _db.SaveChangesAsync(ct);
    }
}
