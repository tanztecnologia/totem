using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TotemAPI.Features.Cart.Application.UseCases;
using TotemAPI.Features.Cart.Infrastructure;
using TotemAPI.Features.Catalog.Application.UseCases;
using TotemAPI.Features.Catalog.Infrastructure;
using TotemAPI.Features.Checkout.Application.Abstractions;
using TotemAPI.Features.Checkout.Application.UseCases;
using TotemAPI.Features.Checkout.Domain;
using TotemAPI.Features.Checkout.Infrastructure;
using TotemAPI.Features.Pos.Application.Abstractions;
using TotemAPI.Features.Pos.Application.UseCases;
using TotemAPI.Features.Pos.Domain;
using Xunit;

namespace TotemAPI.Tests;

public sealed class PosUseCasesTests
{
    [Fact]
    public async Task StartCheckout_com_comanda_nao_chama_TEF_e_cria_pagamento_POS()
    {
        var tenantId = Guid.NewGuid();

        var skuRepo = new InMemorySkuRepository();
        var createSku = new CreateSku(skuRepo);
        var sku = await createSku.HandleAsync(
            new CreateSkuCommand(tenantId, "SKU1", "Item 1", 1500, null, null, true),
            CancellationToken.None
        );

        var cartRepo = new InMemoryCartRepository();
        var createCart = new CreateCart(cartRepo);
        var setItem = new SetCartItem(cartRepo, skuRepo);

        var cart = await createCart.HandleAsync(new CreateCartCommand(tenantId), CancellationToken.None);
        await setItem.HandleAsync(new SetCartItemCommand(tenantId, cart.Id, sku.Id, 1), CancellationToken.None);

        var checkoutRepo = new InMemoryCheckoutRepository();
        var tef = new _ThrowingTefPaymentService();
        var startCheckout = new StartCheckout(skuRepo, checkoutRepo, tef, cartRepo);

        var started = await startCheckout.HandleAsync(
            new StartCheckoutCommand(
                TenantId: tenantId,
                CartId: cart.Id,
                Fulfillment: OrderFulfillment.DineIn,
                PaymentMethod: PaymentMethod.Cash,
                Comanda: "10"
            ),
            CancellationToken.None
        );

        Assert.Equal(OrderStatus.Created, started.OrderStatus);
        Assert.Equal(PaymentStatus.Pending, started.Payment.Status);
        Assert.Equal(PaymentMethod.Cash, started.Payment.Method);
        Assert.Equal("POS", started.Payment.Provider);
        Assert.Null(started.Payment.ProviderReference);
        Assert.Null(started.Payment.PixPayload);
        Assert.Null(started.Payment.PixExpiresAt);
    }

    [Fact]
    public async Task ListPosOrdersByComanda_retorna_pedidos_da_comanda()
    {
        var tenantId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var repo = new InMemoryCheckoutRepository();
        var order = new Order(
            Id: orderId,
            TenantId: tenantId,
            CartId: null,
            Fulfillment: OrderFulfillment.DineIn,
            TotalCents: 2500,
            Status: OrderStatus.Created,
            KitchenStatus: OrderKitchenStatus.Queued,
            Comanda: "99",
            CreatedAt: now,
            UpdatedAt: now,
            QueuedAt: now,
            InPreparationAt: null,
            ReadyAt: null,
            CompletedAt: null,
            CancelledAt: null
        );

        var payment = new Payment(
            Id: Guid.NewGuid(),
            TenantId: tenantId,
            OrderId: orderId,
            Method: PaymentMethod.Cash,
            Status: PaymentStatus.Pending,
            AmountCents: 2500,
            Provider: "POS",
            ProviderReference: string.Empty,
            TransactionId: string.Empty,
            PixPayload: null,
            PixExpiresAt: null,
            CreatedAt: now,
            UpdatedAt: now
        );

        await repo.CreateAsync(order, new List<OrderItem>(), payment, CancellationToken.None);

        var useCase = new ListPosOrdersByComanda(repo);
        var list = await useCase.HandleAsync(new ListPosOrdersByComandaQuery(tenantId, "99", false, 10), CancellationToken.None);

        Assert.Single(list);
        Assert.Equal(orderId, list[0].OrderId);
        Assert.Equal("99", list[0].Comanda);
        Assert.Equal(OrderStatus.Created, list[0].Status);
    }

    [Fact]
    public async Task PayPosOrder_marca_pedido_como_pago_e_aprova_pagamento()
    {
        var tenantId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        ICheckoutRepository repo = new InMemoryCheckoutRepository();
        var order = new Order(
            Id: orderId,
            TenantId: tenantId,
            CartId: null,
            Fulfillment: OrderFulfillment.DineIn,
            TotalCents: 2500,
            Status: OrderStatus.Created,
            KitchenStatus: OrderKitchenStatus.Queued,
            Comanda: "12",
            CreatedAt: now,
            UpdatedAt: now,
            QueuedAt: now,
            InPreparationAt: null,
            ReadyAt: null,
            CompletedAt: null,
            CancelledAt: null
        );

        var paymentId = Guid.NewGuid();
        var payment = new Payment(
            Id: paymentId,
            TenantId: tenantId,
            OrderId: orderId,
            Method: PaymentMethod.Pix,
            Status: PaymentStatus.Pending,
            AmountCents: 2500,
            Provider: "POS",
            ProviderReference: string.Empty,
            TransactionId: string.Empty,
            PixPayload: null,
            PixExpiresAt: null,
            CreatedAt: now,
            UpdatedAt: now
        );

        await repo.CreateAsync(order, new List<OrderItem>(), payment, CancellationToken.None);

        var cashRegister = new _FakeCashRegisterRepository(hasOpenShift: true, tenantId: tenantId);
        var useCase = new PayPosOrder(repo, cashRegister);
        var result = await useCase.HandleAsync(
            new PayPosOrderCommand(tenantId, orderId, PaymentMethod.DebitCard, "PDV-123"),
            CancellationToken.None
        );

        Assert.Equal(orderId, result.OrderId);
        Assert.Equal(OrderStatus.Paid, result.OrderStatus);
        Assert.Equal(PaymentStatus.Approved, result.Payment.Status);
        Assert.Equal(PaymentMethod.DebitCard, result.Payment.Method);
        Assert.Equal("PDV-123", result.Payment.TransactionId);

        var updatedOrder = await repo.GetOrderAsync(tenantId, orderId, CancellationToken.None);
        Assert.NotNull(updatedOrder);
        Assert.Equal(OrderStatus.Paid, updatedOrder!.Status);

        var updatedPayment = await repo.GetPaymentByOrderIdAsync(tenantId, orderId, CancellationToken.None);
        Assert.NotNull(updatedPayment);
        Assert.Equal(PaymentStatus.Approved, updatedPayment!.Status);
        Assert.Equal(PaymentMethod.DebitCard, updatedPayment.Method);
    }

    [Fact]
    public async Task PayPosOrder_sem_caixa_aberto_retorna_erro()
    {
        var tenantId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        ICheckoutRepository repo = new InMemoryCheckoutRepository();
        var order = new Order(
            Id: orderId,
            TenantId: tenantId,
            CartId: null,
            Fulfillment: OrderFulfillment.DineIn,
            TotalCents: 2500,
            Status: OrderStatus.Created,
            KitchenStatus: OrderKitchenStatus.Queued,
            Comanda: "12",
            CreatedAt: now,
            UpdatedAt: now,
            QueuedAt: now,
            InPreparationAt: null,
            ReadyAt: null,
            CompletedAt: null,
            CancelledAt: null
        );

        var paymentId = Guid.NewGuid();
        var payment = new Payment(
            Id: paymentId,
            TenantId: tenantId,
            OrderId: orderId,
            Method: PaymentMethod.Pix,
            Status: PaymentStatus.Pending,
            AmountCents: 2500,
            Provider: "POS",
            ProviderReference: string.Empty,
            TransactionId: string.Empty,
            PixPayload: null,
            PixExpiresAt: null,
            CreatedAt: now,
            UpdatedAt: now
        );

        await repo.CreateAsync(order, new List<OrderItem>(), payment, CancellationToken.None);

        var cashRegister = new _FakeCashRegisterRepository(hasOpenShift: false, tenantId: tenantId);
        var useCase = new PayPosOrder(repo, cashRegister);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => useCase.HandleAsync(new PayPosOrderCommand(tenantId, orderId, PaymentMethod.DebitCard, "PDV-123"), CancellationToken.None)
        );

        Assert.Equal("Caixa fechado. Abra o caixa para receber pagamentos.", ex.Message);
    }
}

file sealed class _FakeCashRegisterRepository : ICashRegisterRepository
{
    public _FakeCashRegisterRepository(bool hasOpenShift, Guid tenantId)
    {
        _hasOpenShift = hasOpenShift;
        _tenantId = tenantId;
    }

    private readonly bool _hasOpenShift;
    private readonly Guid _tenantId;

    public Task<CashRegisterShift?> GetOpenShiftAsync(Guid tenantId, CancellationToken ct)
    {
        if (!_hasOpenShift || tenantId != _tenantId) return Task.FromResult<CashRegisterShift?>(null);

        var now = DateTimeOffset.UtcNow;
        return Task.FromResult<CashRegisterShift?>(
            new CashRegisterShift(
                Id: Guid.NewGuid(),
                TenantId: tenantId,
                Status: CashRegisterShiftStatus.Open,
                OpenedByUserId: Guid.NewGuid(),
                OpenedByEmail: "pdv@empresax.local",
                OpeningCashCents: 0,
                OpenedAt: now,
                ClosedByUserId: null,
                ClosedByEmail: null,
                ClosingCashCents: null,
                TotalSalesCents: null,
                TotalCashSalesCents: null,
                ExpectedCashCents: null,
                ClosedAt: null,
                CreatedAt: now,
                UpdatedAt: now
            )
        );
    }

    public Task<CashRegisterShift?> GetByIdAsync(Guid tenantId, Guid shiftId, CancellationToken ct)
    {
        return Task.FromResult<CashRegisterShift?>(null);
    }

    public Task AddAsync(CashRegisterShift shift, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task UpdateAsync(CashRegisterShift shift, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyDictionary<PaymentMethod, int>> SumApprovedPosPaymentsByMethodAsync(
        Guid tenantId,
        DateTimeOffset fromInclusive,
        DateTimeOffset toInclusive,
        CancellationToken ct
    )
    {
        throw new NotImplementedException();
    }
}

file sealed class _ThrowingTefPaymentService : ITefPaymentService
{
    public Task<TefPixCharge> CreatePixChargeAsync(int amountCents, string reference, CancellationToken ct)
    {
        throw new InvalidOperationException("TEF não deve ser chamado para comanda.");
    }

    public Task<string> StartCardAsync(int amountCents, PaymentMethod method, string reference, CancellationToken ct)
    {
        throw new InvalidOperationException("TEF não deve ser chamado para comanda.");
    }

    public Task<TefPaymentConfirmation> ConfirmAsync(string providerReference, PaymentMethod method, CancellationToken ct)
    {
        throw new InvalidOperationException("TEF não deve ser chamado para comanda.");
    }
}
