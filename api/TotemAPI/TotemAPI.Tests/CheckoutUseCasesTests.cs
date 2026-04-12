using TotemAPI.Features.Catalog.Application.UseCases;
using TotemAPI.Features.Catalog.Infrastructure;
using TotemAPI.Features.Cart.Application.UseCases;
using TotemAPI.Features.Cart.Infrastructure;
using TotemAPI.Features.Checkout.Application.UseCases;
using TotemAPI.Features.Checkout.Domain;
using TotemAPI.Features.Checkout.Infrastructure;
using Xunit;

namespace TotemAPI.Tests;

public sealed class CheckoutUseCasesTests
{
    [Fact]
    public async Task Pix_checkout_e_confirmacao_marcam_pedido_como_pago()
    {
        var tenantId = Guid.NewGuid();

        var skuRepo = new InMemorySkuRepository();
        var createSku = new CreateSku(skuRepo);

        var sku = await createSku.HandleAsync(
            new CreateSkuCommand(tenantId, "X-BURGER", "X Burger", 2500, null, null, true),
            CancellationToken.None
        );

        var checkoutRepo = new InMemoryCheckoutRepository();
        var cartRepo = new InMemoryCartRepository();
        var tef = new FakeTefPaymentService();

        var createCart = new CreateCart(cartRepo);
        var setItem = new SetCartItem(cartRepo, skuRepo);

        var cart = await createCart.HandleAsync(new CreateCartCommand(tenantId), CancellationToken.None);
        await setItem.HandleAsync(new SetCartItemCommand(tenantId, cart.Id, sku.Id, 2), CancellationToken.None);

        var startCheckout = new StartCheckout(skuRepo, checkoutRepo, tef, cartRepo);
        var confirmPayment = new ConfirmPayment(checkoutRepo, tef, cartRepo);
        var getOrder = new GetOrder(checkoutRepo);

        var started = await startCheckout.HandleAsync(
            new StartCheckoutCommand(
                TenantId: tenantId,
                CartId: cart.Id,
                Fulfillment: OrderFulfillment.DineIn,
                PaymentMethod: PaymentMethod.Pix
            ),
            CancellationToken.None
        );

        Assert.Equal(OrderStatus.Created, started.OrderStatus);
        Assert.Equal(5000, started.TotalCents);
        Assert.Equal(PaymentStatus.Pending, started.Payment.Status);
        Assert.NotNull(started.Payment.PixPayload);
        Assert.NotNull(started.Payment.PixExpiresAt);

        var confirmed = await confirmPayment.HandleAsync(
            new ConfirmPaymentCommand(tenantId, started.Payment.Id),
            CancellationToken.None
        );

        Assert.NotNull(confirmed);
        Assert.Equal(OrderStatus.Paid, confirmed!.OrderStatus);
        Assert.Equal(PaymentStatus.Approved, confirmed.Payment.Status);
        Assert.False(string.IsNullOrWhiteSpace(confirmed.Payment.TransactionId));

        var order = await getOrder.HandleAsync(new GetOrderQuery(tenantId, started.OrderId), CancellationToken.None);
        Assert.NotNull(order);
        Assert.Equal(OrderStatus.Paid, order!.Status);
        Assert.Single(order.Items);
    }

    [Fact]
    public async Task Cartao_checkout_e_confirmacao_marcam_pedido_como_pago()
    {
        var tenantId = Guid.NewGuid();

        var skuRepo = new InMemorySkuRepository();
        var createSku = new CreateSku(skuRepo);

        var sku = await createSku.HandleAsync(
            new CreateSkuCommand(tenantId, "COCA-350", "Coca-Cola 350ml", 800, null, null, true),
            CancellationToken.None
        );

        var checkoutRepo = new InMemoryCheckoutRepository();
        var cartRepo = new InMemoryCartRepository();
        var tef = new FakeTefPaymentService();

        var createCart = new CreateCart(cartRepo);
        var setItem = new SetCartItem(cartRepo, skuRepo);

        var cart = await createCart.HandleAsync(new CreateCartCommand(tenantId), CancellationToken.None);
        await setItem.HandleAsync(new SetCartItemCommand(tenantId, cart.Id, sku.Id, 1), CancellationToken.None);

        var startCheckout = new StartCheckout(skuRepo, checkoutRepo, tef, cartRepo);
        var confirmPayment = new ConfirmPayment(checkoutRepo, tef, cartRepo);

        var started = await startCheckout.HandleAsync(
            new StartCheckoutCommand(
                TenantId: tenantId,
                CartId: cart.Id,
                Fulfillment: OrderFulfillment.TakeAway,
                PaymentMethod: PaymentMethod.CreditCard
            ),
            CancellationToken.None
        );

        Assert.Equal(800, started.TotalCents);
        Assert.Equal(PaymentStatus.Pending, started.Payment.Status);
        Assert.Null(started.Payment.PixPayload);
        Assert.NotNull(started.Payment.ProviderReference);

        var confirmed = await confirmPayment.HandleAsync(
            new ConfirmPaymentCommand(tenantId, started.Payment.Id),
            CancellationToken.None
        );

        Assert.NotNull(confirmed);
        Assert.Equal(OrderStatus.Paid, confirmed!.OrderStatus);
        Assert.Equal(PaymentStatus.Approved, confirmed.Payment.Status);
    }

    [Fact]
    public async Task Confirmacao_de_pagamento_respeita_segregacao_por_tenant()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        var skuRepo = new InMemorySkuRepository();
        var createSku = new CreateSku(skuRepo);

        var sku = await createSku.HandleAsync(
            new CreateSkuCommand(tenantA, "X-BURGER", "X Burger", 2500, null, null, true),
            CancellationToken.None
        );

        var checkoutRepo = new InMemoryCheckoutRepository();
        var cartRepo = new InMemoryCartRepository();
        var tef = new FakeTefPaymentService();

        var createCart = new CreateCart(cartRepo);
        var setItem = new SetCartItem(cartRepo, skuRepo);

        var cart = await createCart.HandleAsync(new CreateCartCommand(tenantA), CancellationToken.None);
        await setItem.HandleAsync(new SetCartItemCommand(tenantA, cart.Id, sku.Id, 1), CancellationToken.None);

        var startCheckout = new StartCheckout(skuRepo, checkoutRepo, tef, cartRepo);
        var confirmPayment = new ConfirmPayment(checkoutRepo, tef, cartRepo);

        var started = await startCheckout.HandleAsync(
            new StartCheckoutCommand(
                TenantId: tenantA,
                CartId: cart.Id,
                Fulfillment: OrderFulfillment.DineIn,
                PaymentMethod: PaymentMethod.Pix
            ),
            CancellationToken.None
        );

        var confirmedOtherTenant = await confirmPayment.HandleAsync(
            new ConfirmPaymentCommand(tenantB, started.Payment.Id),
            CancellationToken.None
        );

        Assert.Null(confirmedOtherTenant);
    }
}
