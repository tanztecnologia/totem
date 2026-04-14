using TotemAPI.Features.Checkout.Application.Abstractions;
using TotemAPI.Features.Checkout.Domain;
using TotemAPI.Features.Pos.Application.Abstractions;

namespace TotemAPI.Features.Pos.Application.UseCases;

public sealed class PayPosOrder
{
    public PayPosOrder(ICheckoutRepository repo, ICashRegisterRepository cashRegister)
    {
        _repo = repo;
        _cashRegister = cashRegister;
    }

    private readonly ICheckoutRepository _repo;
    private readonly ICashRegisterRepository _cashRegister;

    public async Task<PayPosOrderResult> HandleAsync(PayPosOrderCommand command, CancellationToken ct)
    {
        if (command.TenantId == Guid.Empty) throw new ArgumentException("Tenant inválido.");
        if (command.OrderId == Guid.Empty) throw new ArgumentException("Pedido inválido.");

        var order = await _repo.GetOrderAsync(command.TenantId, command.OrderId, ct);
        if (order is null) throw new InvalidOperationException("Pedido não encontrado.");
        if (string.IsNullOrWhiteSpace(order.Comanda)) throw new InvalidOperationException("Pedido não pertence a uma comanda.");
        if (order.Status == OrderStatus.Cancelled) throw new InvalidOperationException("Pedido cancelado.");

        var payment = await _repo.GetPaymentByOrderIdAsync(command.TenantId, command.OrderId, ct);
        if (payment is null) throw new InvalidOperationException("Pagamento não encontrado.");

        if (order.Status == OrderStatus.Paid && payment.Status == PaymentStatus.Approved)
        {
            return new PayPosOrderResult(
                OrderId: order.Id,
                OrderStatus: order.Status,
                KitchenStatus: order.KitchenStatus,
                Payment: new PosPaymentResult(payment.Id, payment.Method, payment.Status, payment.AmountCents, payment.TransactionId)
            );
        }

        var shift = await _cashRegister.GetOpenShiftAsync(command.TenantId, ct);
        if (shift is null) throw new InvalidOperationException("Caixa fechado. Abra o caixa para receber pagamentos.");

        var now = DateTimeOffset.UtcNow;
        var transactionId = string.IsNullOrWhiteSpace(command.TransactionId)
            ? $"POS-{now:yyyyMMddHHmmss}-{order.Id:N}"
            : command.TransactionId.Trim();

        var newKitchenStatus = order.KitchenStatus == OrderKitchenStatus.PendingPayment ? OrderKitchenStatus.Queued : order.KitchenStatus;
        var newQueuedAt = order.QueuedAt ?? (newKitchenStatus == OrderKitchenStatus.Queued ? now : null);

        var updatedOrder = order with
        {
            Status = OrderStatus.Paid,
            KitchenStatus = newKitchenStatus,
            UpdatedAt = now,
            QueuedAt = newQueuedAt
        };

        var updatedPayment = payment with
        {
            Method = command.PaymentMethod,
            Status = PaymentStatus.Approved,
            Provider = "POS",
            ProviderReference = string.Empty,
            TransactionId = transactionId,
            UpdatedAt = now
        };

        await _repo.UpdateOrderAsync(updatedOrder, ct);
        await _repo.UpdatePaymentAsync(updatedPayment, ct);

        return new PayPosOrderResult(
            OrderId: updatedOrder.Id,
            OrderStatus: updatedOrder.Status,
            KitchenStatus: updatedOrder.KitchenStatus,
            Payment: new PosPaymentResult(updatedPayment.Id, updatedPayment.Method, updatedPayment.Status, updatedPayment.AmountCents, updatedPayment.TransactionId)
        );
    }
}

public sealed record PayPosOrderCommand(Guid TenantId, Guid OrderId, PaymentMethod PaymentMethod, string? TransactionId);

public sealed record PayPosOrderResult(
    Guid OrderId,
    OrderStatus OrderStatus,
    OrderKitchenStatus KitchenStatus,
    PosPaymentResult Payment
);

public sealed record PosPaymentResult(Guid Id, PaymentMethod Method, PaymentStatus Status, int AmountCents, string TransactionId);
