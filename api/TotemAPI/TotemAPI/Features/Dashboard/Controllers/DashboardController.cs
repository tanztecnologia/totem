using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TotemAPI.Features.Dashboard.Application.UseCases;
using TotemAPI.Features.Identity.Domain;

namespace TotemAPI.Features.Dashboard.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public sealed class DashboardController : ControllerBase
{
    [HttpGet("overview")]
    public async Task<ActionResult<DashboardOverviewResult>> Overview(
        [FromServices] GetDashboardOverview getDashboardOverview,
        [FromQuery] string? from,
        [FromQuery] string? to,
        CancellationToken ct
    )
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        if (!CanUseDashboard()) return Forbid();

        var now = DateTimeOffset.UtcNow;
        var fromParsed = TryParseDateTimeOffset(from, out var fromInclusive) ? fromInclusive : now.AddDays(-7);
        var toParsed = TryParseDateTimeOffset(to, out var toInclusive) ? toInclusive : now;

        try
        {
            var result = await getDashboardOverview.HandleAsync(
                new GetDashboardOverviewQuery(tenantId, fromParsed, toParsed),
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

    [HttpGet("orders")]
    public async Task<ActionResult<DashboardOrdersPageResult>> Orders(
        [FromServices] ListDashboardOrders listDashboardOrders,
        [FromQuery] int limit = 50,
        [FromQuery] DateTimeOffset? cursorUpdatedAt = null,
        [FromQuery] Guid? cursorOrderId = null,
        CancellationToken ct = default
    )
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        if (!CanUseDashboard()) return Forbid();

        try
        {
            var result = await listDashboardOrders.HandleAsync(
                new ListDashboardOrdersQuery(tenantId, limit, cursorUpdatedAt, cursorOrderId),
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

    private bool CanUseDashboard()
    {
        return User.IsInRole(UserRole.Admin.ToString()) || User.IsInRole(UserRole.Staff.ToString());
    }

    private bool TryGetTenantId(out Guid tenantId)
    {
        tenantId = Guid.Empty;
        var claim = User.FindFirstValue("tenant_id");
        return claim is not null && Guid.TryParse(claim, out tenantId);
    }

    private static bool TryParseDateTimeOffset(string? raw, out DateTimeOffset value)
    {
        value = default;
        if (string.IsNullOrWhiteSpace(raw)) return false;
        return DateTimeOffset.TryParse(raw.Trim(), out value);
    }
}
