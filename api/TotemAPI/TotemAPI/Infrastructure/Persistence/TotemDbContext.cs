using Microsoft.EntityFrameworkCore;
using TotemAPI.Features.Catalog.Domain;
using TotemAPI.Features.Identity.Domain;

namespace TotemAPI.Infrastructure.Persistence;

public sealed class TotemDbContext : DbContext
{
    public TotemDbContext(DbContextOptions<TotemDbContext> options)
        : base(options)
    {
    }

    public DbSet<TenantRow> Tenants => Set<TenantRow>();
    public DbSet<UserRow> Users => Set<UserRow>();
    public DbSet<SkuRow> Skus => Set<SkuRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TenantRow>(b =>
        {
            b.ToTable("tenants");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).IsRequired();
            b.Property(x => x.NormalizedName).IsRequired();
            b.Property(x => x.CreatedAt).IsRequired();
            b.HasIndex(x => x.NormalizedName).IsUnique();
        });

        modelBuilder.Entity<UserRow>(b =>
        {
            b.ToTable("users");
            b.HasKey(x => x.Id);
            b.Property(x => x.TenantId).IsRequired();
            b.Property(x => x.Email).IsRequired();
            b.Property(x => x.NormalizedEmail).IsRequired();
            b.Property(x => x.PasswordHash).IsRequired();
            b.Property(x => x.Role).HasConversion<int>().IsRequired();
            b.Property(x => x.CreatedAt).IsRequired();
            b.HasIndex(x => x.NormalizedEmail).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.NormalizedEmail }).IsUnique();
        });

        modelBuilder.Entity<SkuRow>(b =>
        {
            b.ToTable("skus");
            b.HasKey(x => x.Id);
            b.Property(x => x.TenantId).IsRequired();
            b.Property(x => x.Code).IsRequired();
            b.Property(x => x.NormalizedCode).IsRequired();
            b.Property(x => x.Name).IsRequired();
            b.Property(x => x.PriceCents).IsRequired();
            b.Property(x => x.ImageUrl);
            b.Property(x => x.IsActive).IsRequired();
            b.Property(x => x.CreatedAt).IsRequired();
            b.Property(x => x.UpdatedAt).IsRequired();
            b.HasIndex(x => new { x.TenantId, x.NormalizedCode }).IsUnique();
            b.HasIndex(x => x.TenantId);
        });
    }
}

public sealed class TenantRow
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class UserRow
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string NormalizedEmail { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class SkuRow
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string NormalizedCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int PriceCents { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

internal static class TenantMapping
{
    public static Tenant ToDomain(this TenantRow row) => new(row.Id, row.Name, row.CreatedAt);
    public static string NormalizeName(string name) => (name ?? string.Empty).Trim().ToUpperInvariant();
}

internal static class UserMapping
{
    public static User ToDomain(this UserRow row) => new(row.Id, row.TenantId, row.Email, row.PasswordHash, row.Role, row.CreatedAt);
    public static string NormalizeEmail(string email) => (email ?? string.Empty).Trim().ToLowerInvariant();
}

internal static class SkuMapping
{
    public static Sku ToDomain(this SkuRow row) =>
        new(row.Id, row.TenantId, row.Code, row.Name, row.PriceCents, row.ImageUrl, row.IsActive, row.CreatedAt, row.UpdatedAt);

    public static string NormalizeCode(string code) => (code ?? string.Empty).Trim().ToUpperInvariant();
}

