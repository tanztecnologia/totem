using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TotemAPI.Features.Identity.Domain;
using TotemAPI.Features.Kitchen.Application.UseCases;
using TotemAPI.Infrastructure.Auth;

namespace TotemAPI.Features.Kitchen.Controllers;

[ApiController]
[Route("api/admin/kitchen-sla")]
[Authorize]
public sealed class AdminKitchenSlaController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<KitchenSlaResult>> Get([FromServices] GetKitchenSla getKitchenSla, CancellationToken ct)
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        if (!IsAdmin()) return Forbid();

        var result = await getKitchenSla.HandleAsync(new GetKitchenSlaQuery(tenantId), ct);
        return Ok(result);
    }

    [HttpPut]
    public async Task<ActionResult<KitchenSlaResult>> Upsert(
        [FromServices] UpsertKitchenSla upsertKitchenSla,
        [FromBody] UpsertKitchenSlaRequest request,
        CancellationToken ct
    )
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        if (!IsAdmin()) return Forbid();

        try
        {
            var result = await upsertKitchenSla.HandleAsync(
                new UpsertKitchenSlaCommand(
                    TenantId: tenantId,
                    QueuedTargetSeconds: request.QueuedTargetSeconds,
                    PreparationBaseTargetSeconds: request.PreparationBaseTargetSeconds,
                    ReadyTargetSeconds: request.ReadyTargetSeconds
                ),
                ct
            );
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    private bool IsAdmin()
    {
        return User.HasPermission(Permissions.KitchenSlaManage);
    }

    private bool TryGetTenantId(out Guid tenantId)
    {
        tenantId = default;
        var raw = User.FindFirstValue("tenant_id");
        return Guid.TryParse(raw, out tenantId);
    }
}

public sealed record UpsertKitchenSlaRequest(
    int QueuedTargetSeconds,
    int PreparationBaseTargetSeconds,
    int ReadyTargetSeconds
);
