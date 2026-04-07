using Microsoft.EntityFrameworkCore;
using TotemAPI.Features.Identity.Application.Abstractions;
using TotemAPI.Features.Identity.Domain;

namespace TotemAPI.Infrastructure.Persistence;

public sealed class EfUserRepository : IUserRepository
{
    public EfUserRepository(TotemDbContext db)
    {
        _db = db;
    }

    private readonly TotemDbContext _db;

    public async Task<User?> GetByEmailAsync(Guid tenantId, string email, CancellationToken ct)
    {
        var normalized = UserMapping.NormalizeEmail(email);
        if (normalized.Length == 0) return null;

        var row = await _db.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.TenantId == tenantId && x.NormalizedEmail == normalized, ct);
        return row?.ToDomain();
    }

    public async Task<User?> GetByIdAsync(Guid tenantId, Guid userId, CancellationToken ct)
    {
        var row = await _db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == userId && x.TenantId == tenantId, ct);
        return row?.ToDomain();
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken ct)
    {
        var normalized = UserMapping.NormalizeEmail(email);
        if (normalized.Length == 0) return false;
        return await _db.Users.AsNoTracking().AnyAsync(x => x.NormalizedEmail == normalized, ct);
    }

    public async Task AddAsync(User user, CancellationToken ct)
    {
        var row = new UserRow
        {
            Id = user.Id,
            TenantId = user.TenantId,
            Email = user.Email,
            NormalizedEmail = UserMapping.NormalizeEmail(user.Email),
            PasswordHash = user.PasswordHash,
            Role = user.Role,
            CreatedAt = user.CreatedAt,
        };

        _db.Users.Add(row);
        await _db.SaveChangesAsync(ct);
    }
}

