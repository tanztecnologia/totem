using Microsoft.Extensions.Logging;
using TotemAPI.Features.Identity.Application.Abstractions;
using TotemAPI.Features.Identity.Domain;
using TotemAPI.Infrastructure.Logging;

namespace TotemAPI.Features.Identity.Application.UseCases;

public sealed record RegisterUserCommand(
    string TenantName,
    string Email,
    string Password
);

public sealed record AuthResult(
    Guid TenantId,
    Guid UserId,
    string Email,
    UserRole Role,
    IReadOnlyList<string> Permissions,
    string Token
);

public sealed class RegisterUser
{
    public RegisterUser(
        ITenantRepository tenants,
        IUserRepository users,
        IPasswordHasher passwordHasher,
        IJwtTokenService tokenService,
        ILogger<RegisterUser> logger
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
    private readonly ILogger<RegisterUser> _logger;

    public async Task<AuthResult> HandleAsync(RegisterUserCommand command, CancellationToken ct)
    {
        var tenantName = (command.TenantName ?? string.Empty).Trim();
        var email = (command.Email ?? string.Empty).Trim().ToLowerInvariant();
        var password = command.Password ?? string.Empty;

        var maskedEmail = PiiMasker.MaskEmail(email);

        if (tenantName.Length < 2) throw new ArgumentException("TenantName inválido.");
        if (email.Length < 5) throw new ArgumentException("Email inválido.");
        if (password.Length < 6) throw new ArgumentException("Senha inválida.");

        _logger.LogInformation(
            "auth.register.attempt tenant={TenantName} email={MaskedEmail}",
            tenantName, maskedEmail);

        if (await _users.EmailExistsAsync(email, ct))
        {
            _logger.LogWarning(
                "auth.register.failed reason=email_already_exists email={MaskedEmail}",
                maskedEmail);
            throw new InvalidOperationException("Email já cadastrado.");
        }

        var existingTenant = await _tenants.GetByNameAsync(tenantName, ct);
        if (existingTenant is not null)
        {
            _logger.LogWarning(
                "auth.register.failed reason=tenant_already_exists tenant={TenantName} email={MaskedEmail}",
                tenantName, maskedEmail);
            throw new InvalidOperationException("Tenant já existe.");
        }

        var tenant = new Tenant(Guid.NewGuid(), tenantName, DateTimeOffset.UtcNow);
        await _tenants.AddAsync(tenant, ct);

        var user = new User(
            Id: Guid.NewGuid(),
            TenantId: tenant.Id,
            Email: email,
            PasswordHash: _passwordHasher.Hash(password),
            Role: UserRole.Admin,
            CreatedAt: DateTimeOffset.UtcNow
        );
        await _users.AddAsync(user, ct);

        var token = _tokenService.CreateToken(user);
        var permissions = Permissions.ForRole(user.Role);

        _logger.LogInformation(
            "auth.register.success tenantId={TenantId} userId={UserId} email={MaskedEmail}",
            tenant.Id, user.Id, maskedEmail);

        return new AuthResult(user.TenantId, user.Id, user.Email, user.Role, permissions, token);
    }
}
