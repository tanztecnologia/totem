using TotemAPI.Features.Checkout.Domain;

namespace TotemAPI.Features.Dashboard.Application.Abstractions;

public interface IDashboardRepository
{
    Task<DashboardOverviewSnapshot> GetOverviewAsync(
        Guid tenantId,
        DateTimeOffset fromInclusive,
        DateTimeOffset toInclusive,
        CancellationToken ct
    );

    Task<DashboardOrdersPageSnapshot> GetOrdersPageAsync(
        Guid tenantId,
        int limit,
        DateTimeOffset? cursorUpdatedAt,
        Guid? cursorOrderId,
        CancellationToken ct
    );
}

public sealed record DashboardOverviewSnapshot(
    int OrdersCount,
    int PaidOrdersCount,
    int CancelledOrdersCount,
    int RevenueCents,
    IReadOnlyList<DashboardPaymentMethodTotal> PaymentsByMethod,
    IReadOnlyList<DashboardPaymentProviderTotal> PaymentsByProvider,
    IReadOnlyList<DashboardKitchenStatusTotal> OrdersByKitchenStatus
);

public sealed record DashboardPaymentMethodTotal(PaymentMethod Method, int AmountCents, int PaymentsCount);

public sealed record DashboardPaymentProviderTotal(string Provider, int AmountCents, int PaymentsCount);

public sealed record DashboardKitchenStatusTotal(OrderKitchenStatus KitchenStatus, int OrdersCount);

public sealed record DashboardOrderSnapshot(
    Guid OrderId,
    string? Comanda,
    OrderStatus Status,
    OrderKitchenStatus KitchenStatus,
    int TotalCents,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    PaymentStatus? PaymentStatus,
    PaymentMethod? PaymentMethod,
    int? PaymentAmountCents,
    string? PaymentProvider
);

public sealed record DashboardOrdersPageSnapshot(
    IReadOnlyList<DashboardOrderSnapshot> Items,
    DateTimeOffset? NextCursorUpdatedAt,
    Guid? NextCursorOrderId
);
