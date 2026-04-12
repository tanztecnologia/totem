using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TotemAPI.Features.Catalog.Application.UseCases;
using TotemAPI.Features.Identity.Domain;

namespace TotemAPI.Features.Catalog.Controllers;

[ApiController]
[Route("api/skus")]
[Authorize]
public sealed class SkusController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SkuResult>>> List(
        [FromServices] ListSkus listSkus,
        CancellationToken ct
    )
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        if (!CanReadSkus()) return Forbid();

        var result = await listSkus.HandleAsync(new ListSkusQuery(tenantId), ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SkuResult>> Get(
        [FromServices] GetSku getSku,
        [FromRoute] Guid id,
        CancellationToken ct
    )
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        if (!CanReadSkus()) return Forbid();

        try
        {
            var result = await getSku.HandleAsync(new GetSkuQuery(tenantId, id), ct);
            return result is null ? NotFound() : Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult<SkuResult>> Create(
        [FromServices] CreateSku createSku,
        [FromBody] CreateSkuRequest request,
        CancellationToken ct
    )
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        if (!CanWriteSkus()) return Forbid();

        try
        {
            var result = await createSku.HandleAsync(
                new CreateSkuCommand(
                    TenantId: tenantId,
                    Code: request.Code,
                    Name: request.Name,
                    PriceCents: request.PriceCents,
                    AveragePrepSeconds: request.AveragePrepSeconds,
                    ImageUrl: request.ImageUrl,
                    IsActive: request.IsActive
                ),
                ct
            );
            return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
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

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<SkuResult>> Update(
        [FromServices] UpdateSku updateSku,
        [FromRoute] Guid id,
        [FromBody] UpdateSkuRequest request,
        CancellationToken ct
    )
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        if (!CanWriteSkus()) return Forbid();

        try
        {
            var result = await updateSku.HandleAsync(
                new UpdateSkuCommand(
                    TenantId: tenantId,
                    SkuId: id,
                    Code: request.Code,
                    Name: request.Name,
                    PriceCents: request.PriceCents,
                    AveragePrepSeconds: request.AveragePrepSeconds,
                    ImageUrl: request.ImageUrl,
                    IsActive: request.IsActive
                ),
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

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        [FromServices] DeleteSku deleteSku,
        [FromRoute] Guid id,
        CancellationToken ct
    )
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        if (!CanWriteSkus()) return Forbid();

        try
        {
            var deleted = await deleteSku.HandleAsync(new DeleteSkuCommand(tenantId, id), ct);
            return deleted ? NoContent() : NotFound();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    private bool CanReadSkus()
    {
        return User.IsInRole(UserRole.Admin.ToString())
            || User.IsInRole(UserRole.Staff.ToString())
            || User.IsInRole(UserRole.Totem.ToString());
    }

    private bool CanWriteSkus()
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

public sealed record CreateSkuRequest(
    string Code,
    string Name,
    int PriceCents,
    int? AveragePrepSeconds,
    string? ImageUrl,
    bool IsActive
);

public sealed record UpdateSkuRequest(
    string Code,
    string Name,
    int PriceCents,
    int? AveragePrepSeconds,
    string? ImageUrl,
    bool IsActive
);
