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

    public async Task<IReadOnlyList<SkuStockConsumption>> ListStockConsumptionsAsync(Guid tenantId, Guid skuId, CancellationToken ct)
    {
        if (tenantId == Guid.Empty) return Array.Empty<SkuStockConsumption>();
        if (skuId == Guid.Empty) return Array.Empty<SkuStockConsumption>();

        return await _db.SkuStockConsumptions
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.SkuId == skuId)
            .OrderBy(x => x.SourceSkuId)
            .Select(x => x.ToDomain())
            .ToListAsync(ct);
    }

    public async Task ReplaceStockConsumptionsAsync(
        Guid tenantId,
        Guid skuId,
        IReadOnlyList<SkuStockConsumption> items,
        CancellationToken ct
    )
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId inválido.");
        if (skuId == Guid.Empty) throw new ArgumentException("SkuId inválido.");

        var existing = await _db.SkuStockConsumptions.Where(x => x.TenantId == tenantId && x.SkuId == skuId).ToListAsync(ct);
        if (existing.Count > 0) _db.SkuStockConsumptions.RemoveRange(existing);

        foreach (var item in items)
        {
            if (item.TenantId != tenantId) throw new ArgumentException("TenantId inválido.");
            if (item.SkuId != skuId) throw new ArgumentException("SkuId inválido.");
            if (item.SourceSkuId == Guid.Empty) throw new ArgumentException("SourceSkuId inválido.");
            if (item.QuantityBase <= 0) throw new ArgumentException("QuantityBase inválido.");

            _db.SkuStockConsumptions.Add(
                new SkuStockConsumptionRow
                {
                    Id = item.Id == Guid.Empty ? Guid.NewGuid() : item.Id,
                    TenantId = tenantId,
                    SkuId = skuId,
                    SourceSkuId = item.SourceSkuId,
                    QuantityBase = item.QuantityBase,
                }
            );
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<SkuStockLedgerEntry> AddStockLedgerEntryAsync(SkuStockLedgerEntry entry, CancellationToken ct)
    {
        if (entry.TenantId == Guid.Empty) throw new ArgumentException("TenantId inválido.");
        if (entry.SkuId == Guid.Empty) throw new ArgumentException("SkuId inválido.");
        if (entry.DeltaBaseQty == 0) throw new ArgumentException("DeltaBaseQty não pode ser zero.");

        var skuRow = await _db.Skus.SingleOrDefaultAsync(x => x.TenantId == entry.TenantId && x.Id == entry.SkuId, ct);
        if (skuRow is null) throw new InvalidOperationException("SKU não encontrado.");
        if (!skuRow.TracksStock) throw new InvalidOperationException("SKU não controla estoque próprio.");
        if (skuRow.StockBaseUnit is null || skuRow.StockOnHandBaseQty is null)
            throw new InvalidOperationException("Controle de estoque não configurado para o SKU.");

        var stockAfter = skuRow.StockOnHandBaseQty.Value + entry.DeltaBaseQty;
        if (stockAfter < 0) throw new InvalidOperationException("Estoque insuficiente.");

        var persisted = entry with
        {
            Id = entry.Id == Guid.Empty ? Guid.NewGuid() : entry.Id,
            StockAfterBaseQty = stockAfter,
            CreatedAt = entry.CreatedAt == default ? DateTimeOffset.UtcNow : entry.CreatedAt,
        };

        _db.SkuStockLedger.Add(new SkuStockLedgerRow
        {
            Id = persisted.Id,
            TenantId = persisted.TenantId,
            SkuId = persisted.SkuId,
            DeltaBaseQty = persisted.DeltaBaseQty,
            StockAfterBaseQty = persisted.StockAfterBaseQty,
            OriginType = persisted.OriginType,
            OriginId = persisted.OriginId,
            Notes = persisted.Notes,
            ActorUserId = persisted.ActorUserId,
            CreatedAt = persisted.CreatedAt,
        });

        skuRow.StockOnHandBaseQty = stockAfter;
        skuRow.UpdatedAt = persisted.CreatedAt;

        await _db.SaveChangesAsync(ct);
        return persisted;
    }

    public async Task<IReadOnlyList<SkuStockLedgerEntry>> ListStockLedgerAsync(
        Guid tenantId,
        Guid skuId,
        int limit,
        DateTimeOffset? cursorCreatedAt,
        Guid? cursorId,
        CancellationToken ct
    )
    {
        if (limit <= 0) return Array.Empty<SkuStockLedgerEntry>();
        if (limit > 500) limit = 500;

        var query = _db.SkuStockLedger
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.SkuId == skuId);

        if (cursorCreatedAt is not null && cursorId is not null && cursorId != Guid.Empty)
        {
            query = query.Where(x =>
                x.CreatedAt < cursorCreatedAt ||
                (x.CreatedAt == cursorCreatedAt && x.Id.CompareTo(cursorId.Value) < 0));
        }

        return await query
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Take(limit)
            .Select(x => x.ToDomain())
            .ToListAsync(ct);
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
            NfeIcmsOrig = sku.NfeIcmsOrig,
            NfeIcmsCst = sku.NfeIcmsCst,
            NfeIcmsModBc = sku.NfeIcmsModBc,
            NfeIcmsVBc = sku.NfeIcmsVBc,
            NfeIcmsPIcms = sku.NfeIcmsPIcms,
            NfeIcmsVIcms = sku.NfeIcmsVIcms,
            NfePisCst = sku.NfePisCst,
            NfePisVBc = sku.NfePisVBc,
            NfePisPPis = sku.NfePisPPis,
            NfePisVPis = sku.NfePisVPis,
            NfeCofinsCst = sku.NfeCofinsCst,
            NfeCofinsVBc = sku.NfeCofinsVBc,
            NfeCofinsPCofins = sku.NfeCofinsPCofins,
            NfeCofinsVCofins = sku.NfeCofinsVCofins,
            TracksStock = sku.TracksStock,
            StockBaseUnit = sku.StockBaseUnit,
            StockOnHandBaseQty = sku.StockOnHandBaseQty,
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
        row.NfeIcmsOrig = sku.NfeIcmsOrig;
        row.NfeIcmsCst = sku.NfeIcmsCst;
        row.NfeIcmsModBc = sku.NfeIcmsModBc;
        row.NfeIcmsVBc = sku.NfeIcmsVBc;
        row.NfeIcmsPIcms = sku.NfeIcmsPIcms;
        row.NfeIcmsVIcms = sku.NfeIcmsVIcms;
        row.NfePisCst = sku.NfePisCst;
        row.NfePisVBc = sku.NfePisVBc;
        row.NfePisPPis = sku.NfePisPPis;
        row.NfePisVPis = sku.NfePisVPis;
        row.NfeCofinsCst = sku.NfeCofinsCst;
        row.NfeCofinsVBc = sku.NfeCofinsVBc;
        row.NfeCofinsPCofins = sku.NfeCofinsPCofins;
        row.NfeCofinsVCofins = sku.NfeCofinsVCofins;
        row.TracksStock = sku.TracksStock;
        row.StockBaseUnit = sku.StockBaseUnit;
        row.StockOnHandBaseQty = sku.StockOnHandBaseQty;
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
