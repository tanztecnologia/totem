using System.Collections.Concurrent;
using TotemAPI.Features.Identity.Application.Abstractions;
using TotemAPI.Features.Identity.Domain;

namespace TotemAPI.Features.Identity.Infrastructure;

public sealed class InMemoryUserRepository : IUserRepository
{
    private readonly ConcurrentDictionary<Guid, User> _byId = new();
    private readonly ConcurrentDictionary<(Guid TenantId, string Email), Guid> _idByEmail = new();
    private readonly ConcurrentDictionary<string, byte> _emailIndex = new(StringComparer.OrdinalIgnoreCase);

    public Task<User?> GetByEmailAsync(Guid tenantId, string email, CancellationToken ct)
    {
        var key = (tenantId, (email ?? string.Empty).Trim().ToLowerInvariant());
        if (!_idByEmail.TryGetValue(key, out var id)) return Task.FromResult<User?>(null);
        return Task.FromResult(_byId.TryGetValue(id, out var user) ? user : null);
    }

    public Task<User?> GetByIdAsync(Guid tenantId, Guid userId, CancellationToken ct)
    {
        if (!_byId.TryGetValue(userId, out var user)) return Task.FromResult<User?>(null);
        return Task.FromResult(user.TenantId == tenantId ? user : null);
    }

    public Task<bool> EmailExistsAsync(string email, CancellationToken ct)
    {
        var key = (email ?? string.Empty).Trim();
        if (key.Length == 0) return Task.FromResult(false);
        return Task.FromResult(_emailIndex.ContainsKey(key));
    }

    public Task AddAsync(User user, CancellationToken ct)
    {
        if (!_byId.TryAdd(user.Id, user)) throw new InvalidOperationException("Usuário já existe.");

        var emailKey = user.Email.Trim().ToLowerInvariant();
        if (!_emailIndex.TryAdd(emailKey, 0)) throw new InvalidOperationException("Email já cadastrado.");

        if (!_idByEmail.TryAdd((user.TenantId, emailKey), user.Id)) throw new InvalidOperationException("Usuário já existe.");
        return Task.CompletedTask;
    }
}

