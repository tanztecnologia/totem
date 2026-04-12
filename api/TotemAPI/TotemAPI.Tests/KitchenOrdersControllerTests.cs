using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using TotemAPI.Features.Checkout.Application.Abstractions;
using TotemAPI.Features.Checkout.Domain;
using TotemAPI.Features.Checkout.Infrastructure;
using TotemAPI.Features.Kitchen.Application.UseCases;
using TotemAPI.Features.Kitchen.Controllers;

namespace TotemAPI.Tests;

public class KitchenOrdersControllerTests
{
    private static (Guid TenantId, Guid OrderId, KitchenOrdersController Controller, ListKitchenOrders ListUseCase, UpdateKitchenOrderStatus UpdateUseCase) SetupController(string role = "Admin")
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

        var items = new List<OrderItem>();
        var payment = new Payment(Guid.NewGuid(), tenantId, orderId, PaymentMethod.Pix, PaymentStatus.Approved, 1500, "Fake", "", "", null, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

        repo.CreateAsync(order, items, payment, CancellationToken.None).Wait();

        var listUseCase = new ListKitchenOrders(repo);
        var updateUseCase = new UpdateKitchenOrderStatus(repo);

        var controller = new KitchenOrdersController();

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("tenant_id", tenantId.ToString()),
            new Claim(ClaimTypes.Role, role)
        }));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        return (tenantId, orderId, controller, listUseCase, updateUseCase);
    }

    [Fact]
    public async Task List_ReturnsOkWithOrders()
    {
        // Arrange
        var (_, _, controller, listUseCase, _) = SetupController();

        // Act
        var result = await controller.List(listUseCase, new[] { "Queued" }, 50, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var value = Assert.IsAssignableFrom<IReadOnlyList<KitchenOrderResult>>(okResult.Value);
        Assert.Single(value);
        Assert.Equal(OrderKitchenStatus.Queued, value[0].KitchenStatus);
    }

    [Fact]
    public async Task UpdateStatus_ReturnsOkWithUpdatedOrder()
    {
        // Arrange
        var (_, orderId, controller, _, updateUseCase) = SetupController();
        var request = new UpdateKitchenOrderStatusRequest(OrderKitchenStatus.InPreparation);

        // Act
        var result = await controller.UpdateStatus(updateUseCase, orderId, request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var value = Assert.IsType<UpdateKitchenOrderStatusResult>(okResult.Value);
        Assert.Equal(OrderKitchenStatus.InPreparation, value.KitchenStatus);
    }

    [Fact]
    public async Task List_ReturnsForbid_WhenUserIsNotAdminOrStaff()
    {
        // Arrange
        var (_, _, controller, listUseCase, _) = SetupController("Totem");

        // Act
        var result = await controller.List(listUseCase, null, null, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result.Result);
    }
}
