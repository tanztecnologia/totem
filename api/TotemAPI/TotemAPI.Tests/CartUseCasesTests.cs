using TotemAPI.Features.Cart.Application.UseCases;
using TotemAPI.Features.Cart.Infrastructure;
using TotemAPI.Features.Catalog.Application.UseCases;
using TotemAPI.Features.Catalog.Infrastructure;
using TotemAPI.Features.Checkout.Application.UseCases;
using TotemAPI.Features.Checkout.Domain;
using TotemAPI.Features.Checkout.Infrastructure;
using Xunit;

namespace TotemAPI.Tests;

public sealed class CartUseCasesTests
{
    [Fact]
    public async Task Carrinho_permite_add_update_remove_e_clear()
    {
        var skuRepo = new InMemorySkuRepository();
        var createSku = new CreateSku(skuRepo);

        var cartRepo = new InMemoryCartRepository();
        var createCart = new CreateCart(cartRepo);
        var setItem = new SetCartItem(cartRepo, skuRepo);
        var getCart = new GetCart(cartRepo, skuRepo);
        var clearCart = new ClearCart(cartRepo);

        var tenantId = Guid.NewGuid();

        var sku = await createSku.HandleAsync(
            new CreateSkuCommand(tenantId, "X-BURGER", "X Burger", 2500, null, null, true),
            CancellationToken.None
        );

        var cart = await createCart.HandleAsync(new CreateCartCommand(tenantId), CancellationToken.None);

        var add = await setItem.HandleAsync(new SetCartItemCommand(tenantId, cart.Id, sku.Id, 2), CancellationToken.None);
        Assert.True(add);

        var afterAdd = await getCart.HandleAsync(new GetCartQuery(tenantId, cart.Id), CancellationToken.None);
        Assert.NotNull(afterAdd);
        Assert.Equal(5000, afterAdd!.TotalCents);
        Assert.Single(afterAdd.Items);
        Assert.Equal(2, afterAdd.Items[0].Quantity);

        var update = await setItem.HandleAsync(new SetCartItemCommand(tenantId, cart.Id, sku.Id, 3), CancellationToken.None);
        Assert.True(update);

        var afterUpdate = await getCart.HandleAsync(new GetCartQuery(tenantId, cart.Id), CancellationToken.None);
        Assert.NotNull(afterUpdate);
        Assert.Equal(7500, afterUpdate!.TotalCents);
        Assert.Single(afterUpdate.Items);
        Assert.Equal(3, afterUpdate.Items[0].Quantity);

        var remove = await setItem.HandleAsync(new SetCartItemCommand(tenantId, cart.Id, sku.Id, 0), CancellationToken.None);
        Assert.True(remove);

        var afterRemove = await getCart.HandleAsync(new GetCartQuery(tenantId, cart.Id), CancellationToken.None);
        Assert.NotNull(afterRemove);
        Assert.Equal(0, afterRemove!.TotalCents);
        Assert.Empty(afterRemove.Items);

        await setItem.HandleAsync(new SetCartItemCommand(tenantId, cart.Id, sku.Id, 1), CancellationToken.None);
        var cleared = await clearCart.HandleAsync(new ClearCartCommand(tenantId, cart.Id), CancellationToken.None);
        Assert.True(cleared);

        var afterClear = await getCart.HandleAsync(new GetCartQuery(tenantId, cart.Id), CancellationToken.None);
        Assert.NotNull(afterClear);
        Assert.Equal(0, afterClear!.TotalCents);
        Assert.Empty(afterClear.Items);
    }

    [Fact]
    public async Task Carrinho_respeita_segregacao_por_tenant()
    {
        var skuRepo = new InMemorySkuRepository();
        var createSku = new CreateSku(skuRepo);

        var cartRepo = new InMemoryCartRepository();
        var createCart = new CreateCart(cartRepo);
        var setItem = new SetCartItem(cartRepo, skuRepo);
        var getCart = new GetCart(cartRepo, skuRepo);
        var clearCart = new ClearCart(cartRepo);

        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        var skuA = await createSku.HandleAsync(
            new CreateSkuCommand(tenantA, "X-BURGER", "X Burger", 2500, null, null, true),
            CancellationToken.None
        );

        var cartA = await createCart.HandleAsync(new CreateCartCommand(tenantA), CancellationToken.None);

        var otherTenantGet = await getCart.HandleAsync(new GetCartQuery(tenantB, cartA.Id), CancellationToken.None);
        Assert.Null(otherTenantGet);

        var otherTenantSet = await setItem.HandleAsync(new SetCartItemCommand(tenantB, cartA.Id, skuA.Id, 1), CancellationToken.None);
        Assert.Null(otherTenantSet);

        var otherTenantClear = await clearCart.HandleAsync(new ClearCartCommand(tenantB, cartA.Id), CancellationToken.None);
        Assert.Null(otherTenantClear);
    }

    [Fact]
    public async Task Checkout_usando_cartId_limpa_carrinho_apos_pagamento_aprovado()
    {
        var tenantId = Guid.NewGuid();

        var skuRepo = new InMemorySkuRepository();
        var createSku = new CreateSku(skuRepo);

        var sku = await createSku.HandleAsync(
            new CreateSkuCommand(tenantId, "X-BURGER", "X Burger", 2500, null, null, true),
            CancellationToken.None
        );

        var cartRepo = new InMemoryCartRepository();
        var createCart = new CreateCart(cartRepo);
        var setItem = new SetCartItem(cartRepo, skuRepo);
        var getCart = new GetCart(cartRepo, skuRepo);

        var cart = await createCart.HandleAsync(new CreateCartCommand(tenantId), CancellationToken.None);
        await setItem.HandleAsync(new SetCartItemCommand(tenantId, cart.Id, sku.Id, 2), CancellationToken.None);

        var checkoutRepo = new InMemoryCheckoutRepository();
        var tef = new FakeTefPaymentService();
        var startCheckout = new StartCheckout(skuRepo, checkoutRepo, tef, cartRepo);
        var confirmPayment = new ConfirmPayment(checkoutRepo, tef, cartRepo);

        var started = await startCheckout.HandleAsync(
            new StartCheckoutCommand(
                TenantId: tenantId,
                CartId: cart.Id,
                Fulfillment: OrderFulfillment.DineIn,
                PaymentMethod: PaymentMethod.Pix
            ),
            CancellationToken.None
        );

        Assert.Equal(5000, started.TotalCents);

        var beforeConfirm = await getCart.HandleAsync(new GetCartQuery(tenantId, cart.Id), CancellationToken.None);
        Assert.NotNull(beforeConfirm);
        Assert.Single(beforeConfirm!.Items);

        var confirmed = await confirmPayment.HandleAsync(
            new ConfirmPaymentCommand(tenantId, started.Payment.Id),
            CancellationToken.None
        );

        Assert.NotNull(confirmed);
        Assert.Equal(OrderStatus.Paid, confirmed!.OrderStatus);

        var afterConfirm = await getCart.HandleAsync(new GetCartQuery(tenantId, cart.Id), CancellationToken.None);
        Assert.NotNull(afterConfirm);
        Assert.Empty(afterConfirm!.Items);
        Assert.Equal(0, afterConfirm.TotalCents);
    }
}
