namespace TotemAPI.Features.Cart.Domain;

public sealed record ShoppingCart(
    Guid Id,
    Guid TenantId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);
