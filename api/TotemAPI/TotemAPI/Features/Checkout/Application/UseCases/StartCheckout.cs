using System.Diagnostics;
using Microsoft.Extensions.Logging;
using TotemAPI.Features.Catalog.Application.Abstractions;
using TotemAPI.Features.Cart.Application.Abstractions;
using TotemAPI.Features.Checkout.Application.Abstractions;
using TotemAPI.Features.Checkout.Domain;
using TotemAPI.Infrastructure.Logging;
using TotemAPI.Infrastructure.Telemetry;

namespace TotemAPI.Features.Checkout.Application.UseCases;

public sealed record StartCheckoutCommand(
    Guid TenantId,
    Guid CartId,
    OrderFulfillment Fulfillment,
    PaymentMethod PaymentMethod,
    string? Comanda
);

public sealed record CheckoutOrderItemResult(
    Guid SkuId,
    string Code,
    string Name,
    int UnitPriceCents,
    int Quantity,
    int TotalCents
);

public sealed record CheckoutPaymentResult(
    Guid Id,
    PaymentMethod Method,
    PaymentStatus Status,
    int AmountCents,
    string? TransactionId,
    string? Provider,
    string? ProviderReference,
    string? PixPayload,
    DateTimeOffset? PixExpiresAt
);

public sealed record StartCheckoutResult(
    Guid OrderId,
    OrderStatus OrderStatus,
    OrderFulfillment Fulfillment,
    int TotalCents,
    IReadOnlyList<CheckoutOrderItemResult> Items,
    CheckoutPaymentResult Payment
);

public sealed class StartCheckout
{
    public StartCheckout(
        ISkuRepository skus,
        ICheckoutRepository checkout,
        ITefPaymentService tef,
        ICartRepository carts,
        ILogger<StartCheckout> logger
    )
    {
        _skus = skus;
        _checkout = checkout;
        _tef = tef;
        _carts = carts;
        _logger = logger;
    }

    private readonly ISkuRepository _skus;
    private readonly ICheckoutRepository _checkout;
    private readonly ITefPaymentService _tef;
    private readonly ICartRepository _carts;
    private readonly ILogger<StartCheckout> _logger;

    public async Task<StartCheckoutResult> HandleAsync(StartCheckoutCommand command, CancellationToken ct)
    {
        using var activity = TotemActivitySource.Instance.StartActivity("checkout.start");
        activity?.SetTag("tenant.id", command.TenantId.ToString());
        activity?.SetTag("order.payment_method", command.PaymentMethod.ToString());
        activity?.SetTag("order.fulfillment", command.Fulfillment.ToString());

        try
        {
            return await ExecuteAsync(command, activity, ct);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddEvent(new ActivityEvent("exception", tags: new ActivityTagsCollection
            {
                { "exception.type", ex.GetType().FullName ?? string.Empty },
                { "exception.message", ex.Message },
                { "exception.stacktrace", ex.StackTrace ?? string.Empty },
            }));
            throw;
        }
    }

    private async Task<StartCheckoutResult> ExecuteAsync(StartCheckoutCommand command, Activity? activity, CancellationToken ct)
    {
        if (command.TenantId == Guid.Empty) throw new ArgumentException("TenantId inválido.");
        if (command.CartId == Guid.Empty) throw new ArgumentException("CartId inválido.");

        _logger.LogInformation(
            "checkout.start.attempt tenantId={TenantId} cartId={CartId} method={Method} fulfillment={Fulfillment}",
            command.TenantId, command.CartId, command.PaymentMethod, command.Fulfillment);

        var cart = await _carts.GetAsync(command.TenantId, command.CartId, ct);
        if (cart is null) throw new InvalidOperationException("Carrinho não encontrado.");

        var cartItems = await _carts.ListItemsAsync(command.TenantId, command.CartId, ct);
        if (cartItems.Count == 0) throw new ArgumentException("Itens vazios.");

        var now = DateTimeOffset.UtcNow;
        var orderId = Guid.NewGuid();

        var items = new List<OrderItem>(cartItems.Count);
        var itemResults = new List<CheckoutOrderItemResult>(cartItems.Count);

        var totalCents = 0;
        foreach (var item in cartItems)
        {
            if (item.SkuId == Guid.Empty) throw new ArgumentException("SkuId inválido.");
            if (item.Quantity <= 0) throw new ArgumentException("Quantity inválido.");

            var sku = await _skus.GetByIdAsync(command.TenantId, item.SkuId, ct);
            if (sku is null) throw new InvalidOperationException("SKU não encontrado.");
            if (!sku.IsActive) throw new InvalidOperationException("SKU inativo.");

            checked
            {
                var lineTotal = sku.PriceCents * item.Quantity;
                totalCents += lineTotal;

                items.Add(
                    new OrderItem(
                        Id: Guid.NewGuid(),
                        TenantId: command.TenantId,
                        OrderId: orderId,
                        SkuId: sku.Id,
                        SkuCode: sku.Code,
                        SkuName: sku.Name,
                        UnitPriceCents: sku.PriceCents,
                        Quantity: item.Quantity,
                        TotalCents: lineTotal,
                        CreatedAt: now
                    )
                );

                itemResults.Add(
                    new CheckoutOrderItemResult(
                        SkuId: sku.Id,
                        Code: sku.Code,
                        Name: sku.Name,
                        UnitPriceCents: sku.PriceCents,
                        Quantity: item.Quantity,
                        TotalCents: lineTotal
                    )
                );
            }
        }

        if (totalCents <= 0) throw new InvalidOperationException("Total inválido.");
        if (command.PaymentMethod == PaymentMethod.Cash && command.Comanda is null)
        {
            throw new ArgumentException("Cash só é permitido para pedidos de comanda.");
        }

        var order = new Order(
            Id: orderId,
            TenantId: command.TenantId,
            CartId: command.CartId,
            Fulfillment: command.Fulfillment,
            TotalCents: totalCents,
            Status: OrderStatus.Created,
            KitchenStatus: command.Comanda != null ? OrderKitchenStatus.Queued : OrderKitchenStatus.PendingPayment,
            Comanda: command.Comanda,
            CreatedAt: now,
            UpdatedAt: now,
            QueuedAt: command.Comanda != null ? now : null,
            InPreparationAt: null,
            ReadyAt: null,
            CompletedAt: null,
            CancelledAt: null
        );

        var paymentId = Guid.NewGuid();
        var provider = "TEF";
        var providerReference = string.Empty;
        string? pixPayload = null;
        DateTimeOffset? pixExpiresAt = null;

        if (command.Comanda != null)
        {
            provider = "POS";
        }
        else if (command.PaymentMethod == PaymentMethod.Pix)
        {
            var charge = await _tef.CreatePixChargeAsync(
                amountCents: totalCents,
                reference: $"order-{orderId:D}",
                ct: ct
            );
            providerReference = charge.ProviderReference;
            pixPayload = charge.Payload;
            pixExpiresAt = charge.ExpiresAt;
        }
        else
        {
            providerReference = await _tef.StartCardAsync(
                amountCents: totalCents,
                method: command.PaymentMethod,
                reference: $"order-{orderId:D}",
                ct: ct
            );
        }

        var payment = new Payment(
            Id: paymentId,
            TenantId: command.TenantId,
            OrderId: orderId,
            Method: command.PaymentMethod,
            Status: PaymentStatus.Pending,
            AmountCents: totalCents,
            Provider: provider,
            ProviderReference: providerReference,
            TransactionId: string.Empty,
            PixPayload: pixPayload,
            PixExpiresAt: pixExpiresAt,
            CreatedAt: now,
            UpdatedAt: now
        );

        await _checkout.CreateAsync(order, items, payment, ct);

        activity?.SetTag("order.id", orderId.ToString());
        activity?.SetTag("order.total_cents", totalCents);
        activity?.SetTag("order.item_count", itemResults.Count);
        activity?.SetTag("order.payment_id", paymentId.ToString());

        _logger.LogInformation(
            "checkout.start.success tenantId={TenantId} orderId={OrderId} paymentId={PaymentId} " +
            "totalCents={TotalCents} itemCount={ItemCount} method={Method} fulfillment={Fulfillment}",
            command.TenantId, orderId, paymentId,
            totalCents, itemResults.Count, command.PaymentMethod, command.Fulfillment);

        return new StartCheckoutResult(
            OrderId: order.Id,
            OrderStatus: order.Status,
            Fulfillment: order.Fulfillment,
            TotalCents: order.TotalCents,
            Items: itemResults,
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
}
