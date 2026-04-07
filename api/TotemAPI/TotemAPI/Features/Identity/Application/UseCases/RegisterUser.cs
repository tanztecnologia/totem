using TotemAPI.Features.Identity.Application.Abstractions;
using TotemAPI.Features.Identity.Domain;

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
    string Token
);

public sealed class RegisterUser
{
    public RegisterUser(
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

    public async Task<AuthResult> HandleAsync(RegisterUserCommand command, CancellationToken ct)
    {
        var tenantName = (command.TenantName ?? string.Empty).Trim();
        var email = (command.Email ?? string.Empty).Trim().ToLowerInvariant();
        var password = command.Password ?? string.Empty;

        if (tenantName.Length < 2) throw new ArgumentException("TenantName inválido.");
        if (email.Length < 5) throw new ArgumentException("Email inválido.");
        if (password.Length < 6) throw new ArgumentException("Senha inválida.");

        if (await _users.EmailExistsAsync(email, ct)) throw new InvalidOperationException("Email já cadastrado.");

        var existingTenant = await _tenants.GetByNameAsync(tenantName, ct);
        if (existingTenant is not null) throw new InvalidOperationException("Tenant já existe.");

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
        return new AuthResult(user.TenantId, user.Id, user.Email, user.Role, token);
    }
}

