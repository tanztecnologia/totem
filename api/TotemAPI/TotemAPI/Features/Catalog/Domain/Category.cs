namespace TotemAPI.Features.Catalog.Domain;

public sealed record Category(
    Guid Id,
    Guid TenantId,
    string Code,
    string Slug,
    string Name,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);
