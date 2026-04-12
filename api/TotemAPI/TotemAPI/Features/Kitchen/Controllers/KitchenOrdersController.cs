using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TotemAPI.Features.Checkout.Domain;
using TotemAPI.Features.Identity.Domain;
using TotemAPI.Features.Kitchen.Application.UseCases;

namespace TotemAPI.Features.Kitchen.Controllers;

[ApiController]
[Route("api/kitchen/orders")]
[Authorize]
public sealed class KitchenOrdersController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<KitchenOrderResult>>> List(
        [FromServices] ListKitchenOrders listKitchenOrders,
        [FromQuery(Name = "status")] string[]? status,
        [FromQuery] int? limit,
        CancellationToken ct
    )
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        if (!CanKitchen()) return Forbid();

        var statuses = ParseStatuses(status);
        var take = limit ?? 50;

        try
        {
            var result = await listKitchenOrders.HandleAsync(new ListKitchenOrdersQuery(tenantId, statuses, take), ct);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpGet("{orderId:guid}")]
    public async Task<ActionResult<GetKitchenOrderResult>> Get(
        [FromServices] GetKitchenOrder getKitchenOrder,
        [FromRoute] Guid orderId,
        CancellationToken ct
    )
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        if (!CanKitchen()) return Forbid();

        try
        {
            var result = await getKitchenOrder.HandleAsync(new GetKitchenOrderQuery(tenantId, orderId), ct);
            return result is null ? NotFound() : Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{orderId:guid}/status")]
    public async Task<ActionResult<UpdateKitchenOrderStatusResult>> UpdateStatus(
        [FromServices] UpdateKitchenOrderStatus updateKitchenOrderStatus,
        [FromRoute] Guid orderId,
        [FromBody] UpdateKitchenOrderStatusRequest request,
        CancellationToken ct
    )
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        if (!CanKitchen()) return Forbid();

        try
        {
            var result = await updateKitchenOrderStatus.HandleAsync(
                new UpdateKitchenOrderStatusCommand(tenantId, orderId, request.KitchenStatus),
                ct
            );
            return result is null ? NotFound() : Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    private static IReadOnlyList<OrderKitchenStatus> ParseStatuses(string[]? raw)
    {
        if (raw is null || raw.Length == 0)
        {
            return new[] { OrderKitchenStatus.Queued, OrderKitchenStatus.InPreparation, OrderKitchenStatus.Ready };
        }

        var list = new List<OrderKitchenStatus>();
        foreach (var s in raw)
        {
            if (!Enum.TryParse<OrderKitchenStatus>(s, ignoreCase: true, out var parsed)) continue;
            list.Add(parsed);
        }

        return list.Count == 0
            ? new[] { OrderKitchenStatus.Queued, OrderKitchenStatus.InPreparation, OrderKitchenStatus.Ready }
            : list;
    }

    private bool CanKitchen()
    {
        return User.IsInRole(UserRole.Admin.ToString()) || User.IsInRole(UserRole.Staff.ToString());
    }

    private bool TryGetTenantId(out Guid tenantId)
    {
        tenantId = default;
        var raw = User.FindFirstValue("tenant_id");
        return Guid.TryParse(raw, out tenantId);
    }
}

public sealed record UpdateKitchenOrderStatusRequest(
    OrderKitchenStatus KitchenStatus
);

