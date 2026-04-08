namespace TotemAPI.Features.Checkout.Domain;

public sealed record OrderItem(
    Guid Id,
    Guid TenantId,
    Guid OrderId,
    Guid SkuId,
    string SkuCode,
    string SkuName,
    int UnitPriceCents,
    int Quantity,
    int TotalCents,
    DateTimeOffset CreatedAt
);

