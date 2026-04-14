using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TotemAPI.Features.Checkout.Domain;
using TotemAPI.Features.Dashboard.Application.Abstractions;
using TotemAPI.Features.Dashboard.Application.UseCases;
using Xunit;

namespace TotemAPI.Tests;

public sealed class DashboardUseCasesTests
{
    [Fact]
    public async Task GetDashboardOverview_calcula_ticket_medio_e_ordena_listas()
    {
        var tenantId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var snapshot = new DashboardOverviewSnapshot(
            OrdersCount: 10,
            PaidOrdersCount: 4,
            CancelledOrdersCount: 1,
            RevenueCents: 10000,
            PaymentsByMethod: new List<DashboardPaymentMethodTotal>
            {
                new(PaymentMethod.Cash, 4000, 1),
                new(PaymentMethod.Pix, 6000, 3)
            }.AsReadOnly(),
            PaymentsByProvider: new List<DashboardPaymentProviderTotal>
            {
                new("TEF", 6000, 3),
                new("POS", 4000, 1)
            }.AsReadOnly(),
            OrdersByKitchenStatus: new List<DashboardKitchenStatusTotal>
            {
                new(OrderKitchenStatus.Ready, 2),
                new(OrderKitchenStatus.Queued, 5)
            }.AsReadOnly()
        );

        var repo = new _FakeDashboardRepository(snapshot, new List<DashboardOrderSnapshot>().AsReadOnly());
        var useCase = new GetDashboardOverview(repo);

        var result = await useCase.HandleAsync(
            new GetDashboardOverviewQuery(tenantId, now.AddDays(-7), now),
            CancellationToken.None
        );

        Assert.Equal(2500, result.AverageTicketCents);

        Assert.Equal(2, result.PaymentsByMethod.Count);
        Assert.Equal(PaymentMethod.Pix, result.PaymentsByMethod[0].Method);
        Assert.Equal(PaymentMethod.Cash, result.PaymentsByMethod[1].Method);

        Assert.Equal(2, result.OrdersByKitchenStatus.Count);
        Assert.Equal(OrderKitchenStatus.Queued, result.OrdersByKitchenStatus[0].KitchenStatus);
        Assert.Equal(OrderKitchenStatus.Ready, result.OrdersByKitchenStatus[1].KitchenStatus);
    }

    [Fact]
    public async Task ListDashboardOrders_retorna_pedidos_recentes()
    {
        var tenantId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var orders = new List<DashboardOrderSnapshot>
        {
            new(
                OrderId: Guid.NewGuid(),
                Comanda: "10",
                Status: OrderStatus.Paid,
                KitchenStatus: OrderKitchenStatus.Queued,
                TotalCents: 2500,
                CreatedAt: now.AddMinutes(-10),
                UpdatedAt: now.AddMinutes(-1),
                PaymentStatus: PaymentStatus.Approved,
                PaymentMethod: PaymentMethod.DebitCard,
                PaymentAmountCents: 2500,
                PaymentProvider: "POS"
            )
        }.AsReadOnly();

        var repo = new _FakeDashboardRepository(
            overview: new DashboardOverviewSnapshot(
                0,
                0,
                0,
                0,
                new List<DashboardPaymentMethodTotal>().AsReadOnly(),
                new List<DashboardPaymentProviderTotal>().AsReadOnly(),
                new List<DashboardKitchenStatusTotal>().AsReadOnly()
            ),
            orders: orders
        );

        var useCase = new ListDashboardOrders(repo);
        var page = await useCase.HandleAsync(new ListDashboardOrdersQuery(tenantId, 10, null, null), CancellationToken.None);
        var list = page.Items;

        Assert.Single(list);
        Assert.Equal("10", list[0].Comanda);
        Assert.Equal(OrderStatus.Paid, list[0].Status);
        Assert.Equal(PaymentMethod.DebitCard, list[0].PaymentMethod);
        Assert.Equal(2500, list[0].PaymentAmountCents);
    }
}

file sealed class _FakeDashboardRepository : IDashboardRepository
{
    public _FakeDashboardRepository(DashboardOverviewSnapshot overview, IReadOnlyList<DashboardOrderSnapshot> orders)
    {
        _overview = overview;
        _orders = orders;
    }

    private readonly DashboardOverviewSnapshot _overview;
    private readonly IReadOnlyList<DashboardOrderSnapshot> _orders;

    public Task<DashboardOverviewSnapshot> GetOverviewAsync(
        Guid tenantId,
        DateTimeOffset fromInclusive,
        DateTimeOffset toInclusive,
        CancellationToken ct
    )
    {
        return Task.FromResult(_overview);
    }

    public Task<DashboardOrdersPageSnapshot> GetOrdersPageAsync(
        Guid tenantId,
        int limit,
        DateTimeOffset? cursorUpdatedAt,
        Guid? cursorOrderId,
        CancellationToken ct
    )
    {
        return Task.FromResult(new DashboardOrdersPageSnapshot(_orders, null, null));
    }
}
