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

    public async Task AddAsync(Sku sku, CancellationToken ct)
    {
        var row = new SkuRow
        {
            Id = sku.Id,
            TenantId = sku.TenantId,
            Code = sku.Code,
            NormalizedCode = SkuMapping.NormalizeCode(sku.Code),
            Name = sku.Name,
            PriceCents = sku.PriceCents,
            ImageUrl = sku.ImageUrl,
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
        row.NormalizedCode = SkuMapping.NormalizeCode(sku.Code);
        row.Name = sku.Name;
        row.PriceCents = sku.PriceCents;
        row.ImageUrl = sku.ImageUrl;
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

