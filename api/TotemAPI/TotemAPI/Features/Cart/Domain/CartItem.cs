namespace TotemAPI.Features.Cart.Domain;

public sealed record ShoppingCartItem(
    Guid Id,
    Guid TenantId,
    Guid CartId,
    Guid SkuId,
    int Quantity,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);
