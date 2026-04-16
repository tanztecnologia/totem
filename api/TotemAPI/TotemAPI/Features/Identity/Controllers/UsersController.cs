using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TotemAPI.Features.Identity.Application.UseCases;
using TotemAPI.Features.Identity.Domain;
using TotemAPI.Infrastructure.Auth;

namespace TotemAPI.Features.Identity.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public sealed class UsersController : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<CreateUserResult>> Create(
        [FromServices] CreateUser createUser,
        [FromBody] CreateUserRequest request,
        CancellationToken ct
    )
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        if (!User.HasPermission(Permissions.UsersManage)) return Forbid();

        try
        {
            var result = await createUser.HandleAsync(
                new CreateUserCommand(
                    TenantId: tenantId,
                    Email: request.Email,
                    Password: request.Password,
                    Role: request.Role
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

    [HttpGet("me")]
    public ActionResult<object> Me()
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        var email = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email");
        var role = User.FindFirstValue(ClaimTypes.Role);
        var permissions = User.FindAll(ClaimsPrincipalExtensions.PermissionClaimType).Select(x => x.Value).Distinct().ToList().AsReadOnly();
        return Ok(new { tenantId, userId = sub, email, role, permissions });
    }

    private bool TryGetTenantId(out Guid tenantId)
    {
        tenantId = default;
        var raw = User.FindFirstValue("tenant_id");
        return Guid.TryParse(raw, out tenantId);
    }
}

public sealed record CreateUserRequest(
    string Email,
    string Password,
    UserRole Role
);
