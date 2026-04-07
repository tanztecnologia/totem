using Microsoft.AspNetCore.Mvc;
using TotemAPI.Features.Identity.Application.UseCases;

namespace TotemAPI.Features.Identity.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<AuthResult>> Register(
        [FromServices] RegisterUser registerUser,
        [FromBody] RegisterRequest request,
        CancellationToken ct
    )
    {
        try
        {
            var result = await registerUser.HandleAsync(
                new RegisterUserCommand(
                    TenantName: request.TenantName,
                    Email: request.Email,
                    Password: request.Password
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

    [HttpPost("login")]
    public async Task<ActionResult<AuthResult>> Login(
        [FromServices] LoginUser loginUser,
        [FromBody] LoginRequest request,
        CancellationToken ct
    )
    {
        try
        {
            var result = await loginUser.HandleAsync(
                new LoginUserCommand(
                    TenantName: request.TenantName,
                    Email: request.Email,
                    Password: request.Password
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
            return Unauthorized(new { error = ex.Message });
        }
    }
}

public sealed record RegisterRequest(
    string TenantName,
    string Email,
    string Password
);

public sealed record LoginRequest(
    string TenantName,
    string Email,
    string Password
);

