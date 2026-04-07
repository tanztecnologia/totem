using Microsoft.EntityFrameworkCore;
using TotemAPI.Features.Identity.Application.Abstractions;
using TotemAPI.Features.Identity.Domain;

namespace TotemAPI.Infrastructure.Persistence;

public sealed class EfTenantRepository : ITenantRepository
{
    public EfTenantRepository(TotemDbContext db)
    {
        _db = db;
    }

    private readonly TotemDbContext _db;

    public async Task<Tenant?> GetByNameAsync(string name, CancellationToken ct)
    {
        var normalized = TenantMapping.NormalizeName(name);
        if (normalized.Length == 0) return null;

        var row = await _db.Tenants.AsNoTracking().SingleOrDefaultAsync(x => x.NormalizedName == normalized, ct);
        return row?.ToDomain();
    }

    public async Task<Tenant?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var row = await _db.Tenants.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
        return row?.ToDomain();
    }

    public async Task AddAsync(Tenant tenant, CancellationToken ct)
    {
        var row = new TenantRow
        {
            Id = tenant.Id,
            Name = tenant.Name,
            NormalizedName = TenantMapping.NormalizeName(tenant.Name),
            CreatedAt = tenant.CreatedAt,
        };

        _db.Tenants.Add(row);
        await _db.SaveChangesAsync(ct);
    }
}

