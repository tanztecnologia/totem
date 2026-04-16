using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TotemAPI.Features.Identity.Domain;
using TotemAPI.Features.Pos.Application.UseCases;
using TotemAPI.Infrastructure.Auth;

namespace TotemAPI.Features.Pos.Controllers;

[ApiController]
[Route("api/pos/cashier")]
[Authorize]
public sealed class PosCashRegisterController : ControllerBase
{
    [HttpGet("current")]
    public async Task<ActionResult<CashRegisterShiftResult?>> Current(
        [FromServices] GetCurrentCashRegisterShift getCurrent,
        CancellationToken ct
    )
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        if (!CanUsePos()) return Forbid();

        try
        {
            var result = await getCurrent.HandleAsync(tenantId, ct);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("open")]
    public async Task<ActionResult<CashRegisterShiftResult>> Open(
        [FromServices] OpenCashRegisterShift open,
        [FromBody] OpenCashRegisterShiftRequest request,
        CancellationToken ct
    )
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (!CanUsePos()) return Forbid();

        try
        {
            var email = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email") ?? string.Empty;
            var result = await open.HandleAsync(
                new OpenCashRegisterShiftCommand(tenantId, userId, email, request.OpeningCashCents),
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

    [HttpPost("close")]
    public async Task<ActionResult<CloseCashRegisterShiftResult>> Close(
        [FromServices] CloseCashRegisterShift close,
        [FromBody] CloseCashRegisterShiftRequest request,
        CancellationToken ct
    )
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (!CanUsePos()) return Forbid();

        try
        {
            var email = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email") ?? string.Empty;
            var result = await close.HandleAsync(
                new CloseCashRegisterShiftCommand(tenantId, userId, email, request.ClosingCashCents),
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
        return User.HasPermission(Permissions.PosRead) || User.HasPermission(Permissions.PosWrite);
    }

    private bool TryGetTenantId(out Guid tenantId)
    {
        tenantId = Guid.Empty;
        var claim = User.FindFirstValue("tenant_id");
        return claim is not null && Guid.TryParse(claim, out tenantId);
    }

    private bool TryGetUserId(out Guid userId)
    {
        userId = Guid.Empty;
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return sub is not null && Guid.TryParse(sub, out userId);
    }
}

public sealed record OpenCashRegisterShiftRequest(int OpeningCashCents);

public sealed record CloseCashRegisterShiftRequest(int ClosingCashCents);
