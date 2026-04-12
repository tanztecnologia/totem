using Microsoft.EntityFrameworkCore;
using TotemAPI.Features.Kitchen.Application.Abstractions;
using TotemAPI.Features.Kitchen.Domain;

namespace TotemAPI.Infrastructure.Persistence;

public sealed class EfKitchenSlaRepository : IKitchenSlaRepository
{
    public EfKitchenSlaRepository(TotemDbContext db)
    {
        _db = db;
    }

    private readonly TotemDbContext _db;

    public async Task<KitchenSla?> GetAsync(Guid tenantId, CancellationToken ct)
    {
        var row = await _db.KitchenSlas.AsNoTracking().SingleOrDefaultAsync(x => x.TenantId == tenantId, ct);
        return row?.ToDomain();
    }

    public async Task UpsertAsync(KitchenSla sla, CancellationToken ct)
    {
        var row = await _db.KitchenSlas.SingleOrDefaultAsync(x => x.TenantId == sla.TenantId, ct);
        if (row is null)
        {
            _db.KitchenSlas.Add(
                new KitchenSlaRow
                {
                    TenantId = sla.TenantId,
                    QueuedTargetSeconds = sla.QueuedTargetSeconds,
                    PreparationBaseTargetSeconds = sla.PreparationBaseTargetSeconds,
                    ReadyTargetSeconds = sla.ReadyTargetSeconds,
                    UpdatedAt = sla.UpdatedAt,
                }
            );
            await _db.SaveChangesAsync(ct);
            return;
        }

        row.QueuedTargetSeconds = sla.QueuedTargetSeconds;
        row.PreparationBaseTargetSeconds = sla.PreparationBaseTargetSeconds;
        row.ReadyTargetSeconds = sla.ReadyTargetSeconds;
        row.UpdatedAt = sla.UpdatedAt;
        await _db.SaveChangesAsync(ct);
    }
}
