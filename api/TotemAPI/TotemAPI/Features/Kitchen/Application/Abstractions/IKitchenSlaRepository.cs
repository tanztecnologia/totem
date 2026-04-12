using TotemAPI.Features.Kitchen.Domain;

namespace TotemAPI.Features.Kitchen.Application.Abstractions;

public interface IKitchenSlaRepository
{
    Task<KitchenSla?> GetAsync(Guid tenantId, CancellationToken ct);
    Task UpsertAsync(KitchenSla sla, CancellationToken ct);
}
