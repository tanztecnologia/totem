using TotemAPI.Features.Checkout.Application.Abstractions;
using TotemAPI.Features.Checkout.Domain;

namespace TotemAPI.Features.Kitchen.Application.UseCases;

public sealed record UpdateKitchenOrderStatusCommand(
    Guid TenantId,
    Guid OrderId,
    OrderKitchenStatus KitchenStatus
);

public sealed record UpdateKitchenOrderStatusResult(
    Guid OrderId,
    OrderStatus OrderStatus,
    OrderKitchenStatus KitchenStatus,
    DateTimeOffset UpdatedAt
);

public sealed class UpdateKitchenOrderStatus
{
    public UpdateKitchenOrderStatus(ICheckoutRepository checkout)
    {
        _checkout = checkout;
    }

    private readonly ICheckoutRepository _checkout;

    public async Task<UpdateKitchenOrderStatusResult?> HandleAsync(UpdateKitchenOrderStatusCommand command, CancellationToken ct)
    {
        if (command.TenantId == Guid.Empty) throw new ArgumentException("TenantId inválido.");
        if (command.OrderId == Guid.Empty) throw new ArgumentException("OrderId inválido.");

        if (command.KitchenStatus == OrderKitchenStatus.PendingPayment)
            throw new ArgumentException("KitchenStatus inválido.");

        var order = await _checkout.GetOrderAsync(command.TenantId, command.OrderId, ct);
        if (order is null) return null;

        if (order.Status != OrderStatus.Paid && command.KitchenStatus is not OrderKitchenStatus.Cancelled)
            throw new InvalidOperationException("Pedido ainda não está pago.");

        if (order.KitchenStatus == command.KitchenStatus)
        {
            return new UpdateKitchenOrderStatusResult(order.Id, order.Status, order.KitchenStatus, order.UpdatedAt);
        }

        if (!CanTransition(order.KitchenStatus, command.KitchenStatus))
            throw new InvalidOperationException("Transição inválida.");

        var now = DateTimeOffset.UtcNow;
        var nextOrderStatus = order.Status;
        if (command.KitchenStatus == OrderKitchenStatus.Cancelled)
        {
            nextOrderStatus = OrderStatus.Cancelled;
        }

        var updated = order with { Status = nextOrderStatus, KitchenStatus = command.KitchenStatus, UpdatedAt = now };
        await _checkout.UpdateOrderAsync(updated, ct);

        return new UpdateKitchenOrderStatusResult(updated.Id, updated.Status, updated.KitchenStatus, updated.UpdatedAt);
    }

    private static bool CanTransition(OrderKitchenStatus current, OrderKitchenStatus next)
    {
        if (current is OrderKitchenStatus.Completed or OrderKitchenStatus.Cancelled) return false;

        if (next == OrderKitchenStatus.Cancelled) return true;

        return (current, next) switch
        {
            (OrderKitchenStatus.PendingPayment, OrderKitchenStatus.Queued) => true,
            (OrderKitchenStatus.Queued, OrderKitchenStatus.InPreparation) => true,
            (OrderKitchenStatus.InPreparation, OrderKitchenStatus.Ready) => true,
            (OrderKitchenStatus.Ready, OrderKitchenStatus.Completed) => true,
            _ => false,
        };
    }
}

