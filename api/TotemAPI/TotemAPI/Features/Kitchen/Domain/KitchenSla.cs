namespace TotemAPI.Features.Kitchen.Domain;

public sealed record KitchenSla(
    Guid TenantId,
    int QueuedTargetSeconds,
    int PreparationBaseTargetSeconds,
    int ReadyTargetSeconds,
    DateTimeOffset UpdatedAt
);
