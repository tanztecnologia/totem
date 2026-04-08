namespace TotemAPI.Features.Checkout.Domain;

public sealed record Order(
    Guid Id,
    Guid TenantId,
    OrderFulfillment Fulfillment,
    int TotalCents,
    OrderStatus Status,
    DateTimeOffset CreatedAt
);

