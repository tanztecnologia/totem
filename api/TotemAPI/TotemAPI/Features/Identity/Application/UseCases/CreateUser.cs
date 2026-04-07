using TotemAPI.Features.Identity.Application.Abstractions;
using TotemAPI.Features.Identity.Domain;

namespace TotemAPI.Features.Identity.Application.UseCases;

public sealed record CreateUserCommand(
    Guid TenantId,
    string Email,
    string Password,
    UserRole Role
);

public sealed record CreateUserResult(
    Guid TenantId,
    Guid UserId,
    string Email,
    UserRole Role
);

public sealed class CreateUser
{
    public CreateUser(IUserRepository users, IPasswordHasher passwordHasher)
    {
        _users = users;
        _passwordHasher = passwordHasher;
    }

    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;

    public async Task<CreateUserResult> HandleAsync(CreateUserCommand command, CancellationToken ct)
    {
        var email = (command.Email ?? string.Empty).Trim().ToLowerInvariant();
        var password = command.Password ?? string.Empty;

        if (command.TenantId == Guid.Empty) throw new ArgumentException("TenantId inválido.");
        if (email.Length < 5) throw new ArgumentException("Email inválido.");
        if (password.Length < 6) throw new ArgumentException("Senha inválida.");

        if (await _users.EmailExistsAsync(email, ct)) throw new InvalidOperationException("Email já cadastrado.");

        var user = new User(
            Id: Guid.NewGuid(),
            TenantId: command.TenantId,
            Email: email,
            PasswordHash: _passwordHasher.Hash(password),
            Role: command.Role,
            CreatedAt: DateTimeOffset.UtcNow
        );

        await _users.AddAsync(user, ct);
        return new CreateUserResult(user.TenantId, user.Id, user.Email, user.Role);
    }
}

