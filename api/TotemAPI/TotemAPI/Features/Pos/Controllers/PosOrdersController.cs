using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TotemAPI.Features.Checkout.Domain;
using TotemAPI.Features.Identity.Domain;
using TotemAPI.Features.Pos.Application.UseCases;

namespace TotemAPI.Features.Pos.Controllers;

[ApiController]
[Route("api/pos/orders")]
[Authorize]
public sealed class PosOrdersController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PosOrderListItem>>> ListByComanda(
        [FromServices] ListPosOrdersByComanda listPosOrdersByComanda,
        [FromQuery] string comanda,
        [FromQuery] bool includePaid = false,
        [FromQuery] int limit = 50,
        CancellationToken ct = default
    )
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        if (!CanUsePos()) return Forbid();

        try
        {
            var result = await listPosOrdersByComanda.HandleAsync(
                new ListPosOrdersByComandaQuery(tenantId, comanda, includePaid, limit),
                ct
            );
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

    [HttpPost("{orderId:guid}/pay")]
    public async Task<ActionResult<PayPosOrderResult>> Pay(
        [FromServices] PayPosOrder payPosOrder,
        [FromRoute] Guid orderId,
        [FromBody] PayPosOrderRequest request,
        CancellationToken ct
    )
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        if (!CanUsePos()) return Forbid();

        try
        {
            var result = await payPosOrder.HandleAsync(
                new PayPosOrderCommand(tenantId, orderId, request.PaymentMethod, request.TransactionId),
                ct
            );
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

    private bool CanUsePos()
    {
        return User.IsInRole(UserRole.Admin.ToString())
            || User.IsInRole(UserRole.Staff.ToString())
            || User.IsInRole(UserRole.Pdv.ToString());
    }

    private bool TryGetTenantId(out Guid tenantId)
    {
        tenantId = Guid.Empty;
        var claim = User.FindFirstValue("tenant_id");
        return claim is not null && Guid.TryParse(claim, out tenantId);
    }
}

public sealed record PayPosOrderRequest(PaymentMethod PaymentMethod, string? TransactionId);

