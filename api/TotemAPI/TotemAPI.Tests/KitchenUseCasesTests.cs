using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using TotemAPI.Features.Catalog.Infrastructure;
using TotemAPI.Features.Checkout.Application.Abstractions;
using TotemAPI.Features.Checkout.Domain;
using TotemAPI.Features.Kitchen.Application.UseCases;
using TotemAPI.Features.Checkout.Infrastructure;
using TotemAPI.Features.Kitchen.Infrastructure;

namespace TotemAPI.Tests;

public class KitchenUseCasesTests
{
    private static (Guid TenantId, Guid OrderId, ICheckoutRepository Repo) SetupRepo()
    {
        var repo = new InMemoryCheckoutRepository();
        var tenantId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        
        var order = new Order(
            Id: orderId,
            TenantId: tenantId,
            CartId: Guid.NewGuid(),
            Fulfillment: OrderFulfillment.TakeAway,
            TotalCents: 1500,
            Status: OrderStatus.Paid,
            KitchenStatus: OrderKitchenStatus.Queued,
            CreatedAt: now,
            UpdatedAt: now,
            QueuedAt: now,
            InPreparationAt: null,
            ReadyAt: null,
            CompletedAt: null,
            CancelledAt: null
        );

        var items = new List<OrderItem>
        {
            new OrderItem(Guid.NewGuid(), tenantId, orderId, Guid.NewGuid(), "SKU1", "Item 1", 1500, 1, 1500, DateTimeOffset.UtcNow)
        };

        var payment = new Payment(Guid.NewGuid(), tenantId, orderId, PaymentMethod.Pix, PaymentStatus.Approved, 1500, "Fake", "", "", null, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

        repo.CreateAsync(order, items, payment, CancellationToken.None).Wait();

        return (tenantId, orderId, repo);
    }

    [Fact]
    public async Task ListKitchenOrders_ReturnsOrders_WithMatchingStatus()
    {
        // Arrange
        var (tenantId, _, repo) = SetupRepo();
        var useCase = new ListKitchenOrders(repo, new InMemorySkuRepository(), new InMemoryKitchenSlaRepository());
        var query = new ListKitchenOrdersQuery(tenantId, new[] { OrderKitchenStatus.Queued }, 10);

        // Act
        var result = await useCase.HandleAsync(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(OrderKitchenStatus.Queued, result[0].KitchenStatus);
    }

    [Fact]
    public async Task GetKitchenOrder_ReturnsOrderDetails()
    {
        // Arrange
        var (tenantId, orderId, repo) = SetupRepo();
        var useCase = new GetKitchenOrder(repo, new InMemorySkuRepository(), new InMemoryKitchenSlaRepository());
        var query = new GetKitchenOrderQuery(tenantId, orderId);

        // Act
        var result = await useCase.HandleAsync(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(orderId, result.OrderId);
        Assert.Single(result.Items);
        Assert.Equal("Item 1", result.Items[0].Name);
    }

    [Fact]
    public async Task UpdateKitchenOrderStatus_UpdatesStatusAndSetsUpdatedAt()
    {
        // Arrange
        var (tenantId, orderId, repo) = SetupRepo();
        var useCase = new UpdateKitchenOrderStatus(repo);
        var command = new UpdateKitchenOrderStatusCommand(tenantId, orderId, OrderKitchenStatus.InPreparation);

        // Act
        var result = await useCase.HandleAsync(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(OrderKitchenStatus.InPreparation, result.KitchenStatus);

        var updatedOrder = await repo.GetOrderAsync(tenantId, orderId, CancellationToken.None);
        Assert.NotNull(updatedOrder);
        Assert.Equal(OrderKitchenStatus.InPreparation, updatedOrder.KitchenStatus);
    }

    [Fact]
    public async Task GetKitchenSla_Retorna_Defaults_Quando_Nao_Configurado()
    {
        var slas = new InMemoryKitchenSlaRepository();
        var get = new GetKitchenSla(slas);
        var result = await get.HandleAsync(new GetKitchenSlaQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(120, result.QueuedTargetSeconds);
        Assert.Equal(480, result.PreparationBaseTargetSeconds);
        Assert.Equal(120, result.ReadyTargetSeconds);
        Assert.Null(result.UpdatedAt);
    }

    [Fact]
    public async Task UpsertKitchenSla_Persiste_E_Get_Retorna_Valores()
    {
        var tenantId = Guid.NewGuid();
        var slas = new InMemoryKitchenSlaRepository();
        var upsert = new UpsertKitchenSla(slas);
        var get = new GetKitchenSla(slas);

        await upsert.HandleAsync(new UpsertKitchenSlaCommand(tenantId, 10, 20, 30), CancellationToken.None);
        var result = await get.HandleAsync(new GetKitchenSlaQuery(tenantId), CancellationToken.None);

        Assert.Equal(10, result.QueuedTargetSeconds);
        Assert.Equal(20, result.PreparationBaseTargetSeconds);
        Assert.Equal(30, result.ReadyTargetSeconds);
        Assert.NotNull(result.UpdatedAt);
    }
}
