using TotemAPI.Features.Checkout.Domain;

namespace TotemAPI.Features.Checkout.Application.Abstractions;

public interface ICheckoutRepository
{
    Task CreateAsync(Order order, IReadOnlyList<OrderItem> items, Payment payment, CancellationToken ct);
    Task<Order?> GetOrderAsync(Guid tenantId, Guid orderId, CancellationToken ct);
    Task<IReadOnlyList<OrderItem>> ListOrderItemsAsync(Guid tenantId, Guid orderId, CancellationToken ct);
    Task<Payment?> GetPaymentAsync(Guid tenantId, Guid paymentId, CancellationToken ct);
    Task UpdatePaymentAsync(Payment payment, CancellationToken ct);
    Task UpdateOrderAsync(Order order, CancellationToken ct);
}

