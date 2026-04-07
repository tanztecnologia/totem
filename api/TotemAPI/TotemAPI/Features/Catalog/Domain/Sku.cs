namespace TotemAPI.Features.Catalog.Domain;

public sealed record Sku(
    Guid Id,
    Guid TenantId,
    string Code,
    string Name,
    int PriceCents,
    string? ImageUrl,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);
