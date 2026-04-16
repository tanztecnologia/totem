using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TotemAPI.Features.Catalog.Application.UseCases;
using TotemAPI.Features.Catalog.Domain;
using TotemAPI.Features.Identity.Domain;
using TotemAPI.Infrastructure.Auth;
using System.IdentityModel.Tokens.Jwt;

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

    [HttpGet("by-code")]
    public async Task<ActionResult<SkuResult>> GetByCode(
        [FromServices] GetSkuByCode getSkuByCode,
        [FromQuery] string code,
        CancellationToken ct
    )
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        if (!CanReadSkus()) return Forbid();

        try
        {
            var result = await getSkuByCode.HandleAsync(new GetSkuByCodeQuery(tenantId, code), ct);
            return result is null ? NotFound() : Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("search")]
    public async Task<ActionResult<SkuSearchPageResult>> Search(
        [FromServices] SearchSkusPage searchSkusPage,
        [FromQuery] string? query,
        [FromQuery] int limit = 50,
        [FromQuery] string? cursorCode = null,
        [FromQuery] Guid? cursorId = null,
        [FromQuery] bool includeInactive = true,
        CancellationToken ct = default
    )
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        if (!CanReadSkus()) return Forbid();

        try
        {
            var result = await searchSkusPage.HandleAsync(
                new SearchSkusPageQuery(
                    TenantId: tenantId,
                    Query: query,
                    Limit: limit,
                    CursorCode: cursorCode,
                    CursorId: cursorId,
                    IncludeInactive: includeInactive
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
                    CategoryCode: request.CategoryCode,
                    Name: request.Name,
                    PriceCents: request.PriceCents,
                    AveragePrepSeconds: request.AveragePrepSeconds,
                    ImageUrl: request.ImageUrl,
                    TracksStock: request.TracksStock,
                    StockBaseUnit: request.StockBaseUnit,
                    StockOnHandBaseQty: request.StockOnHandBaseQty,
                    IsActive: request.IsActive,
                    ActorUserId: TryGetUserId(out var actorId) ? actorId : null
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
                    CategoryCode: request.CategoryCode,
                    Name: request.Name,
                    PriceCents: request.PriceCents,
                    AveragePrepSeconds: request.AveragePrepSeconds,
                    ImageUrl: request.ImageUrl,
                    TracksStock: request.TracksStock,
                    StockBaseUnit: request.StockBaseUnit,
                    StockOnHandBaseQty: request.StockOnHandBaseQty,
                    IsActive: request.IsActive,
                    ActorUserId: TryGetUserId(out var actorId) ? actorId : null
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

    [HttpGet("{id:guid}/stock/consumptions")]
    public async Task<ActionResult<IReadOnlyList<SkuStockConsumptionResult>>> ListStockConsumptions(
        [FromServices] ListSkuStockConsumptions listSkuStockConsumptions,
        [FromRoute] Guid id,
        CancellationToken ct
    )
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        if (!CanReadSkus()) return Forbid();

        try
        {
            var result = await listSkuStockConsumptions.HandleAsync(new ListSkuStockConsumptionsQuery(tenantId, id), ct);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id:guid}/stock/consumptions")]
    public async Task<ActionResult<IReadOnlyList<SkuStockConsumptionResult>>> ReplaceStockConsumptions(
        [FromServices] ReplaceSkuStockConsumptions replaceSkuStockConsumptions,
        [FromRoute] Guid id,
        [FromBody] ReplaceSkuStockConsumptionsRequest request,
        CancellationToken ct
    )
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        if (!CanWriteSkus()) return Forbid();

        try
        {
            var result = await replaceSkuStockConsumptions.HandleAsync(
                new ReplaceSkuStockConsumptionsCommand(
                    TenantId: tenantId,
                    SkuId: id,
                    Items: (request.Items ?? Array.Empty<ReplaceSkuStockConsumptionItem>()).ToList().AsReadOnly()
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

    [HttpPost("{id:guid}/stock/entry")]
    public async Task<ActionResult<SkuResult>> AddStockEntry(
        [FromServices] AddSkuStockEntry addSkuStockEntry,
        [FromRoute] Guid id,
        [FromBody] AddSkuStockEntryRequest request,
        CancellationToken ct
    )
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        if (!CanWriteSkus()) return Forbid();

        try
        {
            var actorUserId = TryGetUserId(out var uid) ? uid : (Guid?)null;
            var result = await addSkuStockEntry.HandleAsync(
                new AddSkuStockEntryCommand(tenantId, id, request.Quantity, request.Unit, actorUserId, request.Notes),
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

    [HttpGet("{id:guid}/stock/ledger")]
    public async Task<ActionResult<IReadOnlyList<SkuStockLedgerEntryResult>>> ListStockLedger(
        [FromServices] ListSkuStockLedger listSkuStockLedger,
        [FromRoute] Guid id,
        [FromQuery] int limit = 50,
        [FromQuery] DateTimeOffset? cursorCreatedAt = null,
        [FromQuery] Guid? cursorId = null,
        CancellationToken ct = default
    )
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        if (!CanReadSkus()) return Forbid();

        try
        {
            var result = await listSkuStockLedger.HandleAsync(
                new ListSkuStockLedgerQuery(tenantId, id, limit, cursorCreatedAt, cursorId),
                ct
            );
            return result is null ? NotFound() : Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
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

    [HttpGet("{id:guid}/images")]
    public async Task<ActionResult<IReadOnlyList<SkuImageResult>>> ListImages(
        [FromServices] ListSkuImages listSkuImages,
        [FromRoute] Guid id,
        CancellationToken ct
    )
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        if (!CanReadSkus()) return Forbid();

        try
        {
            var result = await listSkuImages.HandleAsync(new ListSkuImagesQuery(tenantId, id), ct);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id:guid}/images")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<SkuImageResult>> UploadImage(
        [FromServices] UploadSkuImage uploadSkuImage,
        [FromRoute] Guid id,
        [FromForm] UploadSkuImageRequest request,
        CancellationToken ct
    )
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        if (!CanWriteSkus()) return Forbid();
        if (request.File is null || request.File.Length == 0) return BadRequest(new { error = "Arquivo inválido." });

        try
        {
            await using var stream = request.File.OpenReadStream();
            var result = await uploadSkuImage.HandleAsync(
                new UploadSkuImageCommand(
                    tenantId,
                    id,
                    request.File.FileName,
                    request.File.ContentType ?? "application/octet-stream",
                    stream
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
    }

    [HttpDelete("{id:guid}/images/{imageId:guid}")]
    public async Task<IActionResult> DeleteImage(
        [FromServices] DeleteSkuImage deleteSkuImage,
        [FromRoute] Guid id,
        [FromRoute] Guid imageId,
        CancellationToken ct
    )
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        if (!CanWriteSkus()) return Forbid();

        try
        {
            var deleted = await deleteSkuImage.HandleAsync(new DeleteSkuImageCommand(tenantId, id, imageId), ct);
            return deleted ? NoContent() : NotFound();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    private bool CanReadSkus()
    {
        return User.HasPermission(Permissions.CatalogRead);
    }

    private bool CanWriteSkus()
    {
        return User.HasPermission(Permissions.CatalogWrite);
    }


    private bool TryGetUserId(out Guid userId)
    {
        userId = default;
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(raw, out userId);
    }

    private bool TryGetTenantId(out Guid tenantId)
    {
        tenantId = default;
        var raw = User.FindFirstValue("tenant_id");
        return Guid.TryParse(raw, out tenantId);
    }
}

public sealed record CreateSkuRequest(
    string CategoryCode,
    string Name,
    int PriceCents,
    int? AveragePrepSeconds,
    string? ImageUrl,
    bool? TracksStock,
    StockBaseUnit? StockBaseUnit,
    decimal? StockOnHandBaseQty,
    bool IsActive
);

public sealed record UpdateSkuRequest(
    string CategoryCode,
    string Name,
    int PriceCents,
    int? AveragePrepSeconds,
    string? ImageUrl,
    bool? TracksStock,
    StockBaseUnit? StockBaseUnit,
    decimal? StockOnHandBaseQty,
    bool IsActive
);

public sealed record ReplaceSkuStockConsumptionsRequest(
    IReadOnlyList<ReplaceSkuStockConsumptionItem> Items
);

public sealed record AddSkuStockEntryRequest(
    decimal Quantity,
    string Unit,
    string? Notes = null
);

public sealed record UploadSkuImageRequest(IFormFile File);
