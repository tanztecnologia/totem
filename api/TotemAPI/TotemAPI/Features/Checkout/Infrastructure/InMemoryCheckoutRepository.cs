using TotemAPI.Features.Checkout.Application.Abstractions;
using TotemAPI.Features.Checkout.Domain;

namespace TotemAPI.Features.Checkout.Infrastructure;

public sealed class InMemoryCheckoutRepository : ICheckoutRepository
{
    private readonly Dictionary<Guid, Order> _orders = new();
    private readonly Dictionary<Guid, List<OrderItem>> _orderItemsByOrderId = new();
    private readonly Dictionary<Guid, Payment> _payments = new();

    public Task CreateAsync(Order order, IReadOnlyList<OrderItem> items, Payment payment, CancellationToken ct)
    {
        _orders[order.Id] = order;
        _orderItemsByOrderId[order.Id] = items.ToList();
        _payments[payment.Id] = payment;
        return Task.CompletedTask;
    }

    public Task<Order?> GetOrderAsync(Guid tenantId, Guid orderId, CancellationToken ct)
    {
        if (!_orders.TryGetValue(orderId, out var order)) return Task.FromResult<Order?>(null);
        return Task.FromResult(order.TenantId == tenantId ? order : null);
    }

    public Task<IReadOnlyList<OrderItem>> ListOrderItemsAsync(Guid tenantId, Guid orderId, CancellationToken ct)
    {
        if (!_orderItemsByOrderId.TryGetValue(orderId, out var items)) return Task.FromResult<IReadOnlyList<OrderItem>>(Array.Empty<OrderItem>());
        return Task.FromResult<IReadOnlyList<OrderItem>>(items.Where(x => x.TenantId == tenantId).ToList());
    }

    public Task<Payment?> GetPaymentAsync(Guid tenantId, Guid paymentId, CancellationToken ct)
    {
        if (!_payments.TryGetValue(paymentId, out var payment)) return Task.FromResult<Payment?>(null);
        return Task.FromResult(payment.TenantId == tenantId ? payment : null);
    }

    public Task UpdatePaymentAsync(Payment payment, CancellationToken ct)
    {
        _payments[payment.Id] = payment;
        return Task.CompletedTask;
    }

    public Task UpdateOrderAsync(Order order, CancellationToken ct)
    {
        _orders[order.Id] = order;
        return Task.CompletedTask;
    }
}

