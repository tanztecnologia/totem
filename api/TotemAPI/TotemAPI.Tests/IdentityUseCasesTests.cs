using Microsoft.Extensions.Options;
using TotemAPI.Features.Identity.Application.UseCases;
using TotemAPI.Features.Identity.Domain;
using TotemAPI.Features.Identity.Infrastructure;
using Xunit;

namespace TotemAPI.Tests;

public sealed class IdentityUseCasesTests
{
    [Fact]
    public async Task Register_e_login_funcionam_por_tenant()
    {
        var tenants = new InMemoryTenantRepository();
        var users = new InMemoryUserRepository();
        var hasher = new Pbkdf2PasswordHasher();
        var jwt = new JwtTokenService(
            Options.Create(
                new JwtOptions
                {
                    Issuer = "TZTotem",
                    Audience = "TZTotem",
                    Key = "dev-only-change-me-please-dev-only-change-me-please-32bytes-min",
                }
            )
        );

        var register = new RegisterUser(tenants, users, hasher, jwt);
        var login = new LoginUser(tenants, users, hasher, jwt);

        var reg = await register.HandleAsync(
            new RegisterUserCommand("Restaurante A", "admin@a.com", "123456"),
            CancellationToken.None
        );

        Assert.NotEqual(Guid.Empty, reg.TenantId);
        Assert.NotEqual(Guid.Empty, reg.UserId);
        Assert.Equal("admin@a.com", reg.Email);
        Assert.False(string.IsNullOrWhiteSpace(reg.Token));

        var auth = await login.HandleAsync(
            new LoginUserCommand("Restaurante A", "admin@a.com", "123456"),
            CancellationToken.None
        );

        Assert.Equal(reg.TenantId, auth.TenantId);
        Assert.Equal(reg.UserId, auth.UserId);
        Assert.False(string.IsNullOrWhiteSpace(auth.Token));
    }

    [Fact]
    public async Task Nao_permite_email_duplicado()
    {
        var tenants = new InMemoryTenantRepository();
        var users = new InMemoryUserRepository();
        var hasher = new Pbkdf2PasswordHasher();
        var jwt = new JwtTokenService(
            Options.Create(
                new JwtOptions
                {
                    Issuer = "TZTotem",
                    Audience = "TZTotem",
                    Key = "dev-only-change-me-please-dev-only-change-me-please-32bytes-min",
                }
            )
        );

        var register = new RegisterUser(tenants, users, hasher, jwt);

        await register.HandleAsync(
            new RegisterUserCommand("Restaurante A", "admin@a.com", "123456"),
            CancellationToken.None
        );

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () =>
                await register.HandleAsync(
                    new RegisterUserCommand("Restaurante B", "admin@a.com", "123456"),
                    CancellationToken.None
                )
        );
    }

    [Fact]
    public async Task Login_com_senha_errada_falha()
    {
        var tenants = new InMemoryTenantRepository();
        var users = new InMemoryUserRepository();
        var hasher = new Pbkdf2PasswordHasher();
        var jwt = new JwtTokenService(
            Options.Create(
                new JwtOptions
                {
                    Issuer = "TZTotem",
                    Audience = "TZTotem",
                    Key = "dev-only-change-me-please-dev-only-change-me-please-32bytes-min",
                }
            )
        );

        var register = new RegisterUser(tenants, users, hasher, jwt);
        var login = new LoginUser(tenants, users, hasher, jwt);

        await register.HandleAsync(
            new RegisterUserCommand("Restaurante A", "admin@a.com", "123456"),
            CancellationToken.None
        );

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () =>
                await login.HandleAsync(
                    new LoginUserCommand("Restaurante A", "admin@a.com", "senha-errada"),
                    CancellationToken.None
                )
        );
    }

    [Fact]
    public async Task Admin_consegue_criar_usuario_totem_no_mesmo_tenant()
    {
        var tenants = new InMemoryTenantRepository();
        var users = new InMemoryUserRepository();
        var hasher = new Pbkdf2PasswordHasher();
        var jwt = new JwtTokenService(
            Options.Create(
                new JwtOptions
                {
                    Issuer = "TZTotem",
                    Audience = "TZTotem",
                    Key = "dev-only-change-me-please-dev-only-change-me-please-32bytes-min",
                }
            )
        );

        var register = new RegisterUser(tenants, users, hasher, jwt);
        var createUser = new CreateUser(users, hasher);

        var reg = await register.HandleAsync(
            new RegisterUserCommand("Restaurante A", "admin@a.com", "123456"),
            CancellationToken.None
        );

        var totem = await createUser.HandleAsync(
            new CreateUserCommand(
                TenantId: reg.TenantId,
                Email: "totem-1@a.com",
                Password: "123456",
                Role: UserRole.Totem
            ),
            CancellationToken.None
        );

        Assert.Equal(reg.TenantId, totem.TenantId);
        Assert.Equal("totem-1@a.com", totem.Email);
        Assert.Equal(UserRole.Totem, totem.Role);
    }

    [Fact]
    public async Task Admin_consegue_criar_usuario_waiter_no_mesmo_tenant()
    {
        var tenants = new InMemoryTenantRepository();
        var users = new InMemoryUserRepository();
        var hasher = new Pbkdf2PasswordHasher();
        var jwt = new JwtTokenService(
            Options.Create(
                new JwtOptions
                {
                    Issuer = "TZTotem",
                    Audience = "TZTotem",
                    Key = "dev-only-change-me-please-dev-only-change-me-please-32bytes-min",
                }
            )
        );

        var register = new RegisterUser(tenants, users, hasher, jwt);
        var createUser = new CreateUser(users, hasher);

        var reg = await register.HandleAsync(
            new RegisterUserCommand("Restaurante A", "admin@a.com", "123456"),
            CancellationToken.None
        );

        var waiter = await createUser.HandleAsync(
            new CreateUserCommand(
                TenantId: reg.TenantId,
                Email: "waiter-1@a.com",
                Password: "123456",
                Role: UserRole.Waiter
            ),
            CancellationToken.None
        );

        Assert.Equal(reg.TenantId, waiter.TenantId);
        Assert.Equal("waiter-1@a.com", waiter.Email);
        Assert.Equal(UserRole.Waiter, waiter.Role);
    }
}
