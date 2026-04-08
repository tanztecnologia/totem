using TotemAPI.Features.Catalog.Application.Abstractions;
using TotemAPI.Features.Checkout.Application.Abstractions;
using TotemAPI.Features.Checkout.Domain;

namespace TotemAPI.Features.Checkout.Application.UseCases;

public sealed record StartCheckoutItem(
    Guid SkuId,
    int Quantity
);

public sealed record StartCheckoutCommand(
    Guid TenantId,
    OrderFulfillment Fulfillment,
    PaymentMethod PaymentMethod,
    IReadOnlyList<StartCheckoutItem> Items
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
        ITefPaymentService tef
    )
    {
        _skus = skus;
        _checkout = checkout;
        _tef = tef;
    }

    private readonly ISkuRepository _skus;
    private readonly ICheckoutRepository _checkout;
    private readonly ITefPaymentService _tef;

    public async Task<StartCheckoutResult> HandleAsync(StartCheckoutCommand command, CancellationToken ct)
    {
        if (command.TenantId == Guid.Empty) throw new ArgumentException("TenantId inválido.");
        if (command.Items is null || command.Items.Count == 0) throw new ArgumentException("Items inválido.");

        var now = DateTimeOffset.UtcNow;
        var orderId = Guid.NewGuid();

        var items = new List<OrderItem>(command.Items.Count);
        var itemResults = new List<CheckoutOrderItemResult>(command.Items.Count);

        var totalCents = 0;
        foreach (var item in command.Items)
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

        var order = new Order(
            Id: orderId,
            TenantId: command.TenantId,
            Fulfillment: command.Fulfillment,
            TotalCents: totalCents,
            Status: OrderStatus.Created,
            CreatedAt: now
        );

        var paymentId = Guid.NewGuid();
        var provider = "TEF";
        var providerReference = string.Empty;
        string? pixPayload = null;
        DateTimeOffset? pixExpiresAt = null;

        if (command.PaymentMethod == PaymentMethod.Pix)
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

