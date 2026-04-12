namespace TotemAPI.Features.Checkout.Domain;

public sealed record Order(
    Guid Id,
    Guid TenantId,
    Guid? CartId,
    OrderFulfillment Fulfillment,
    int TotalCents,
    OrderStatus Status,
    OrderKitchenStatus KitchenStatus,
    string? Comanda,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? QueuedAt,
    DateTimeOffset? InPreparationAt,
    DateTimeOffset? ReadyAt,
    DateTimeOffset? CompletedAt,
    DateTimeOffset? CancelledAt
);
