using TotemAPI.Features.Identity.Domain;

namespace TotemAPI.Features.Identity.Application.Abstractions;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(Guid tenantId, string email, CancellationToken ct);
    Task<User?> GetByIdAsync(Guid tenantId, Guid userId, CancellationToken ct);
    Task<bool> EmailExistsAsync(string email, CancellationToken ct);
    Task AddAsync(User user, CancellationToken ct);
}

