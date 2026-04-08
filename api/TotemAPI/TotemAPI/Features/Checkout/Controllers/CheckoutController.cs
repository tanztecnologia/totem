using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TotemAPI.Features.Checkout.Application.UseCases;
using TotemAPI.Features.Checkout.Domain;
using TotemAPI.Features.Identity.Domain;

namespace TotemAPI.Features.Checkout.Controllers;

[ApiController]
[Route("api/checkout")]
[Authorize]
public sealed class CheckoutController : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<StartCheckoutResult>> Start(
        [FromServices] StartCheckout startCheckout,
        [FromBody] StartCheckoutRequest request,
        CancellationToken ct
    )
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        if (!CanCheckout()) return Forbid();

        try
        {
            var result = await startCheckout.HandleAsync(
                new StartCheckoutCommand(
                    TenantId: tenantId,
                    Fulfillment: request.Fulfillment,
                    PaymentMethod: request.PaymentMethod,
                    Items: request.Items
                ),
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
        catch (OverflowException)
        {
            return BadRequest(new { error = "Total inválido." });
        }
    }

    [HttpPost("payments/{paymentId:guid}/confirm")]
    public async Task<ActionResult<ConfirmPaymentResult>> Confirm(
        [FromServices] ConfirmPayment confirmPayment,
        [FromRoute] Guid paymentId,
        CancellationToken ct
    )
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        if (!CanCheckout()) return Forbid();

        try
        {
            var result = await confirmPayment.HandleAsync(new ConfirmPaymentCommand(tenantId, paymentId), ct);
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

    [HttpGet("orders/{orderId:guid}")]
    public async Task<ActionResult<GetOrderResult>> GetOrder(
        [FromServices] GetOrder getOrder,
        [FromRoute] Guid orderId,
        CancellationToken ct
    )
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        if (!CanCheckout()) return Forbid();

        try
        {
            var result = await getOrder.HandleAsync(new GetOrderQuery(tenantId, orderId), ct);
            return result is null ? NotFound() : Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    private bool CanCheckout()
    {
        return User.IsInRole(UserRole.Admin.ToString())
            || User.IsInRole(UserRole.Staff.ToString())
            || User.IsInRole(UserRole.Totem.ToString());
    }

    private bool TryGetTenantId(out Guid tenantId)
    {
        tenantId = default;
        var raw = User.FindFirstValue("tenant_id");
        return Guid.TryParse(raw, out tenantId);
    }
}

public sealed record StartCheckoutRequest(
    OrderFulfillment Fulfillment,
    PaymentMethod PaymentMethod,
    IReadOnlyList<StartCheckoutItem> Items
);

