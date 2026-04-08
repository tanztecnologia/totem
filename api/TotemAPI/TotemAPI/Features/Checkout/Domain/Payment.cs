namespace TotemAPI.Features.Checkout.Domain;

public sealed record Payment(
    Guid Id,
    Guid TenantId,
    Guid OrderId,
    PaymentMethod Method,
    PaymentStatus Status,
    int AmountCents,
    string Provider,
    string ProviderReference,
    string TransactionId,
    string? PixPayload,
    DateTimeOffset? PixExpiresAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);

