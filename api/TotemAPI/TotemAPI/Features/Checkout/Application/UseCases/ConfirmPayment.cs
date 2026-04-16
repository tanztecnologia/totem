using TotemAPI.Features.Checkout.Application.Abstractions;
using TotemAPI.Features.Checkout.Domain;
using TotemAPI.Features.Cart.Application.Abstractions;
using TotemAPI.Features.Catalog.Application.Abstractions;
using TotemAPI.Features.Catalog.Domain;

namespace TotemAPI.Features.Checkout.Application.UseCases;

public sealed record ConfirmPaymentCommand(
    Guid TenantId,
    Guid PaymentId
);

public sealed record ConfirmPaymentResult(
    Guid OrderId,
    OrderStatus OrderStatus,
    CheckoutPaymentResult Payment
);

public sealed class ConfirmPayment
{
    public ConfirmPayment(
        ICheckoutRepository checkout,
        ITefPaymentService tef,
        ICartRepository carts,
        ISkuRepository skus
    )
    {
        _checkout = checkout;
        _tef = tef;
        _carts = carts;
        _skus = skus;
    }

    private readonly ICheckoutRepository _checkout;
    private readonly ITefPaymentService _tef;
    private readonly ICartRepository _carts;
    private readonly ISkuRepository _skus;

    public async Task<ConfirmPaymentResult?> HandleAsync(ConfirmPaymentCommand command, CancellationToken ct)
    {
        if (command.TenantId == Guid.Empty) throw new ArgumentException("TenantId inválido.");
        if (command.PaymentId == Guid.Empty) throw new ArgumentException("PaymentId inválido.");

        var payment = await _checkout.GetPaymentAsync(command.TenantId, command.PaymentId, ct);
        if (payment is null) return null;

        var order = await _checkout.GetOrderAsync(command.TenantId, payment.OrderId, ct);
        if (order is null) return null;

        if (payment.Status == PaymentStatus.Approved)
        {
            return new ConfirmPaymentResult(
                OrderId: order.Id,
                OrderStatus: order.Status,
                Payment: new CheckoutPaymentResult(
                    Id: payment.Id,
                    Method: payment.Method,
                    Status: payment.Status,
                    AmountCents: payment.AmountCents,
                    TransactionId: string.IsNullOrWhiteSpace(payment.TransactionId) ? null : payment.TransactionId,
                    Provider: string.IsNullOrWhiteSpace(payment.Provider) ? null : payment.Provider,
                    ProviderReference: string.IsNullOrWhiteSpace(payment.ProviderReference) ? null : payment.ProviderReference,
                    PixPayload: payment.PixPayload,
                    PixExpiresAt: payment.PixExpiresAt
                )
            );
        }

        if (string.IsNullOrWhiteSpace(payment.ProviderReference)) throw new InvalidOperationException("Pagamento inválido.");

        var confirmation = await _tef.ConfirmAsync(payment.ProviderReference, payment.Method, ct);
        var now = DateTimeOffset.UtcNow;

        var newPayment = payment with
        {
            Status = confirmation.IsApproved ? PaymentStatus.Approved : PaymentStatus.Declined,
            TransactionId = confirmation.TransactionId ?? string.Empty,
            UpdatedAt = now,
        };

        var newOrder = order;
        if (confirmation.IsApproved && order.Status != OrderStatus.Paid)
        {
            var nextKitchenStatus = order.KitchenStatus == OrderKitchenStatus.PendingPayment
                ? OrderKitchenStatus.Queued
                : order.KitchenStatus;
            var queuedAt = order.QueuedAt;
            if (nextKitchenStatus == OrderKitchenStatus.Queued && queuedAt is null) queuedAt = now;
            newOrder = order with { Status = OrderStatus.Paid, KitchenStatus = nextKitchenStatus, UpdatedAt = now, QueuedAt = queuedAt };
        }
        else if (confirmation.IsApproved && order.UpdatedAt != now)
        {
            var queuedAt = order.QueuedAt;
            if (order.KitchenStatus == OrderKitchenStatus.Queued && queuedAt is null) queuedAt = now;
            newOrder = order with { UpdatedAt = now, QueuedAt = queuedAt };
        }

        if (confirmation.IsApproved && newOrder != order && newOrder.Status == OrderStatus.Paid)
        {
            await ApplyStockForOrderAsync(command.TenantId, newOrder.Id, ct);
        }

        await _checkout.UpdatePaymentAsync(newPayment, ct);
        if (newOrder != order) await _checkout.UpdateOrderAsync(newOrder, ct);

        if (confirmation.IsApproved && newOrder.CartId is not null)
        {
            await _carts.ClearAsync(command.TenantId, newOrder.CartId.Value, ct);
            await _carts.TouchAsync(command.TenantId, newOrder.CartId.Value, DateTimeOffset.UtcNow, ct);
        }

        return new ConfirmPaymentResult(
            OrderId: newOrder.Id,
            OrderStatus: newOrder.Status,
            Payment: new CheckoutPaymentResult(
                Id: newPayment.Id,
                Method: newPayment.Method,
                Status: newPayment.Status,
                AmountCents: newPayment.AmountCents,
                TransactionId: string.IsNullOrWhiteSpace(newPayment.TransactionId) ? null : newPayment.TransactionId,
                Provider: string.IsNullOrWhiteSpace(newPayment.Provider) ? null : newPayment.Provider,
                ProviderReference: string.IsNullOrWhiteSpace(newPayment.ProviderReference) ? null : newPayment.ProviderReference,
                PixPayload: newPayment.PixPayload,
                PixExpiresAt: newPayment.PixExpiresAt
            )
        );
    }

    private async Task ApplyStockForOrderAsync(Guid tenantId, Guid orderId, CancellationToken ct)
    {
        var items = await _checkout.ListOrderItemsAsync(tenantId, orderId, ct);
        if (items.Count == 0) return;

        var deltas = new Dictionary<Guid, decimal>();

        foreach (var item in items)
        {
            if (item.SkuId == Guid.Empty) continue;
            if (item.Quantity <= 0) continue;

            var consumptions = await _skus.ListStockConsumptionsAsync(tenantId, item.SkuId, ct);
            if (consumptions.Count > 0)
            {
                foreach (var c in consumptions)
                {
                    var delta = -c.QuantityBase * item.Quantity;
                    if (delta == 0) continue;
                    deltas[c.SourceSkuId] = deltas.TryGetValue(c.SourceSkuId, out var cur) ? cur + delta : delta;
                }
                continue;
            }

            var sku = await _skus.GetByIdAsync(tenantId, item.SkuId, ct);
            if (sku is null) continue;
            if (!sku.TracksStock) continue;
            if (sku.StockBaseUnit is null || sku.StockOnHandBaseQty is null) continue;
            if (sku.StockBaseUnit.Value != StockBaseUnit.Unit) continue;

            var deltaSelf = -(decimal)item.Quantity;
            deltas[sku.Id] = deltas.TryGetValue(sku.Id, out var existing) ? existing + deltaSelf : deltaSelf;
        }

        var now = DateTimeOffset.UtcNow;
        foreach (var kv in deltas)
        {
            await _skus.AddStockLedgerEntryAsync(new SkuStockLedgerEntry(
                Id: Guid.NewGuid(),
                TenantId: tenantId,
                SkuId: kv.Key,
                DeltaBaseQty: kv.Value,
                StockAfterBaseQty: 0,
                OriginType: StockLedgerOriginType.OrderPayment,
                OriginId: orderId,
                Notes: null,
                ActorUserId: null,
                CreatedAt: now
            ), ct);
        }
    }
}
