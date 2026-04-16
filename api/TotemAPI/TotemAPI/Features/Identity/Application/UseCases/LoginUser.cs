using Microsoft.Extensions.Logging;
using TotemAPI.Features.Identity.Application.Abstractions;
using TotemAPI.Features.Identity.Domain;
using TotemAPI.Infrastructure.Logging;

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
        IJwtTokenService tokenService,
        ILogger<LoginUser> logger
    )
    {
        _tenants = tenants;
        _users = users;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _logger = logger;
    }

    private readonly ITenantRepository _tenants;
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _tokenService;
    private readonly ILogger<LoginUser> _logger;

    public async Task<AuthResult> HandleAsync(LoginUserCommand command, CancellationToken ct)
    {
        var tenantName = (command.TenantName ?? string.Empty).Trim();
        var email = (command.Email ?? string.Empty).Trim().ToLowerInvariant();
        var password = command.Password ?? string.Empty;

        var maskedEmail = PiiMasker.MaskEmail(email);

        if (tenantName.Length < 2) throw new ArgumentException("TenantName inválido.");
        if (email.Length < 5) throw new ArgumentException("Email inválido.");
        if (password.Length < 1) throw new ArgumentException("Senha inválida.");

        _logger.LogInformation(
            "auth.login.attempt tenant={TenantName} email={MaskedEmail}",
            tenantName, maskedEmail);

        var tenant = await _tenants.GetByNameAsync(tenantName, ct);
        if (tenant is null)
        {
            _logger.LogWarning(
                "auth.login.failed reason=tenant_not_found tenant={TenantName} email={MaskedEmail}",
                tenantName, maskedEmail);
            throw new InvalidOperationException("Credenciais inválidas.");
        }

        var user = await _users.GetByEmailAsync(tenant.Id, email, ct);
        if (user is null)
        {
            _logger.LogWarning(
                "auth.login.failed reason=user_not_found tenantId={TenantId} email={MaskedEmail}",
                tenant.Id, maskedEmail);
            throw new InvalidOperationException("Credenciais inválidas.");
        }

        if (!_passwordHasher.Verify(password, user.PasswordHash))
        {
            _logger.LogWarning(
                "auth.login.failed reason=invalid_password tenantId={TenantId} userId={UserId} email={MaskedEmail}",
                tenant.Id, user.Id, maskedEmail);
            throw new InvalidOperationException("Credenciais inválidas.");
        }

        var token = _tokenService.CreateToken(user);
        var permissions = Permissions.ForRole(user.Role);

        _logger.LogInformation(
            "auth.login.success tenantId={TenantId} userId={UserId} role={Role} email={MaskedEmail}",
            user.TenantId, user.Id, user.Role, maskedEmail);

        return new AuthResult(user.TenantId, user.Id, user.Email, user.Role, permissions, token);
    }
}
