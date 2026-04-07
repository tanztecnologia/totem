using TotemAPI.Features.Identity.Application.Abstractions;
using TotemAPI.Features.Identity.Domain;

namespace TotemAPI.Features.Identity.Application.UseCases;

public sealed record LoginUserCommand(
    string TenantName,
    string Email,
    string Password
);

public sealed class LoginUser
{
    public LoginUser(
        ITenantRepository tenants,
        IUserRepository users,
        IPasswordHasher passwordHasher,
        IJwtTokenService tokenService
    )
    {
        _tenants = tenants;
        _users = users;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    private readonly ITenantRepository _tenants;
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _tokenService;

    public async Task<AuthResult> HandleAsync(LoginUserCommand command, CancellationToken ct)
    {
        var tenantName = (command.TenantName ?? string.Empty).Trim();
        var email = (command.Email ?? string.Empty).Trim().ToLowerInvariant();
        var password = command.Password ?? string.Empty;

        if (tenantName.Length < 2) throw new ArgumentException("TenantName inválido.");
        if (email.Length < 5) throw new ArgumentException("Email inválido.");
        if (password.Length < 1) throw new ArgumentException("Senha inválida.");

        var tenant = await _tenants.GetByNameAsync(tenantName, ct);
        if (tenant is null) throw new InvalidOperationException("Credenciais inválidas.");

        var user = await _users.GetByEmailAsync(tenant.Id, email, ct);
        if (user is null) throw new InvalidOperationException("Credenciais inválidas.");

        if (!_passwordHasher.Verify(password, user.PasswordHash)) throw new InvalidOperationException("Credenciais inválidas.");

        var token = _tokenService.CreateToken(user);
        return new AuthResult(user.TenantId, user.Id, user.Email, user.Role, token);
    }
}

