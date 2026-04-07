namespace TotemAPI.Features.Identity.Domain;

public sealed record Tenant(
    Guid Id,
    string Name,
    DateTimeOffset CreatedAt
);

