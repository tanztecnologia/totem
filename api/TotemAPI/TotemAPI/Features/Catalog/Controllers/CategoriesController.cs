using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TotemAPI.Features.Catalog.Application.UseCases;
using TotemAPI.Features.Identity.Domain;

namespace TotemAPI.Features.Catalog.Controllers;

[ApiController]
[Route("api/categories")]
[Authorize]
public sealed class CategoriesController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CategoryResult>>> List(
        [FromServices] ListCategories listCategories,
        [FromQuery] bool includeInactive = true,
        CancellationToken ct = default
    )
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        if (!CanReadCategories()) return Forbid();

        try
        {
            var result = await listCategories.HandleAsync(new ListCategoriesQuery(tenantId, includeInactive), ct);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{code}")]
    public async Task<ActionResult<CategoryResult>> GetByCode(
        [FromServices] GetCategoryByCode getCategoryByCode,
        [FromRoute] string code,
        CancellationToken ct
    )
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        if (!CanReadCategories()) return Forbid();

        try
        {
            var result = await getCategoryByCode.HandleAsync(new GetCategoryByCodeQuery(tenantId, code), ct);
            return result is null ? NotFound() : Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult<CategoryResult>> Create(
        [FromServices] CreateCategory createCategory,
        [FromBody] CreateCategoryRequest request,
        CancellationToken ct
    )
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        if (!CanWriteCategories()) return Forbid();

        try
        {
            var result = await createCategory.HandleAsync(
                new CreateCategoryCommand(tenantId, request.Name, request.Slug, request.IsActive),
                ct
            );
            return CreatedAtAction(nameof(GetByCode), new { code = result.Code }, result);
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

    [HttpPut("{code}")]
    public async Task<ActionResult<CategoryResult>> Update(
        [FromServices] UpdateCategory updateCategory,
        [FromRoute] string code,
        [FromBody] UpdateCategoryRequest request,
        CancellationToken ct
    )
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        if (!CanWriteCategories()) return Forbid();

        try
        {
            var result = await updateCategory.HandleAsync(
                new UpdateCategoryCommand(tenantId, code, request.Name, request.Slug, request.IsActive),
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

    [HttpDelete("{code}")]
    public async Task<IActionResult> Delete(
        [FromServices] DeleteCategory deleteCategory,
        [FromRoute] string code,
        CancellationToken ct
    )
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        if (!CanWriteCategories()) return Forbid();

        try
        {
            var deleted = await deleteCategory.HandleAsync(new DeleteCategoryCommand(tenantId, code), ct);
            return deleted ? NoContent() : NotFound();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    private bool CanReadCategories()
    {
        return User.IsInRole(UserRole.Admin.ToString())
            || User.IsInRole(UserRole.Staff.ToString())
            || User.IsInRole(UserRole.Totem.ToString())
            || User.IsInRole(UserRole.Waiter.ToString());
    }

    private bool CanWriteCategories()
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

public sealed record CreateCategoryRequest(string Name, string? Slug, bool IsActive);

public sealed record UpdateCategoryRequest(string Name, string? Slug, bool IsActive);
