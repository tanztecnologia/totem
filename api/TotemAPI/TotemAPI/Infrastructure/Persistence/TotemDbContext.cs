using System.Linq;
using Microsoft.EntityFrameworkCore;
using TotemAPI.Features.Cart.Domain;
using TotemAPI.Features.Catalog.Domain;
using TotemAPI.Features.Checkout.Domain;
using TotemAPI.Features.Identity.Domain;
using TotemAPI.Features.Kitchen.Domain;
using TotemAPI.Features.Pos.Domain;

namespace TotemAPI.Infrastructure.Persistence;

public sealed class TotemDbContext : DbContext
{
    public TotemDbContext(DbContextOptions<TotemDbContext> options)
        : base(options)
    {
    }

    public DbSet<TenantRow> Tenants => Set<TenantRow>();
    public DbSet<UserRow> Users => Set<UserRow>();
    public DbSet<CategoryRow> Categories => Set<CategoryRow>();
    public DbSet<SkuRow> Skus => Set<SkuRow>();
    public DbSet<OrderRow> Orders => Set<OrderRow>();
    public DbSet<OrderItemRow> OrderItems => Set<OrderItemRow>();
    public DbSet<PaymentRow> Payments => Set<PaymentRow>();
    public DbSet<CartRow> Carts => Set<CartRow>();
    public DbSet<CartItemRow> CartItems => Set<CartItemRow>();
    public DbSet<KitchenSlaRow> KitchenSlas => Set<KitchenSlaRow>();
    public DbSet<CashRegisterShiftRow> CashRegisterShifts => Set<CashRegisterShiftRow>();

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
            b.Property(x => x.CategoryCode).IsRequired();
            b.Property(x => x.Code).IsRequired();
            b.Property(x => x.NormalizedCode).IsRequired();
            b.Property(x => x.Name).IsRequired();
            b.Property(x => x.PriceCents).IsRequired();
            b.Property(x => x.AveragePrepSeconds);
            b.Property(x => x.ImageUrl);
            b.Property(x => x.NfeCProd);
            b.Property(x => x.NfeCEan);
            b.Property(x => x.NfeCfop);
            b.Property(x => x.NfeUCom);
            b.Property(x => x.NfeQCom);
            b.Property(x => x.NfeVUnCom);
            b.Property(x => x.NfeVProd);
            b.Property(x => x.NfeCEanTrib);
            b.Property(x => x.NfeUTrib);
            b.Property(x => x.NfeQTrib);
            b.Property(x => x.NfeVUnTrib);
            b.Property(x => x.IsActive).IsRequired();
            b.Property(x => x.CreatedAt).IsRequired();
            b.Property(x => x.UpdatedAt).IsRequired();
            b.HasIndex(x => new { x.TenantId, x.NormalizedCode }).IsUnique();
            b.HasIndex(x => x.TenantId);
            b.HasIndex(x => new { x.TenantId, x.CategoryCode });
        });

        modelBuilder.Entity<CategoryRow>(b =>
        {
            b.ToTable("categories");
            b.HasKey(x => x.Id);
            b.Property(x => x.TenantId).IsRequired();
            b.Property(x => x.Code).IsRequired();
            b.Property(x => x.Slug).IsRequired();
            b.Property(x => x.Name).IsRequired();
            b.Property(x => x.IsActive).IsRequired();
            b.Property(x => x.CreatedAt).IsRequired();
            b.Property(x => x.UpdatedAt).IsRequired();
            b.HasIndex(x => x.TenantId);
            b.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.Slug }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.IsActive });
        });

        modelBuilder.Entity<OrderRow>(b =>
        {
            b.ToTable("orders");
            b.HasKey(x => x.Id);
            b.Property(x => x.TenantId).IsRequired();
            b.Property(x => x.CartId);
            b.Property(x => x.Fulfillment).HasConversion<int>().IsRequired();
            b.Property(x => x.TotalCents).IsRequired();
            b.Property(x => x.Status).HasConversion<int>().IsRequired();
            b.Property(x => x.CreatedAt).IsRequired();
            b.Property(x => x.KitchenStatus).HasConversion<int>().IsRequired();
            b.Property(x => x.Comanda);
            b.Property(x => x.UpdatedAt).IsRequired();
            b.Property(x => x.QueuedAt);
            b.Property(x => x.InPreparationAt);
            b.Property(x => x.ReadyAt);
            b.Property(x => x.CompletedAt);
            b.Property(x => x.CancelledAt);
            b.HasIndex(x => x.TenantId);
            b.HasIndex(x => new { x.TenantId, x.CreatedAt });
            b.HasIndex(x => new { x.TenantId, x.UpdatedAt });
        });

        modelBuilder.Entity<KitchenSlaRow>(b =>
        {
            b.ToTable("kitchen_sla");
            b.HasKey(x => x.TenantId);
            b.Property(x => x.QueuedTargetSeconds).IsRequired();
            b.Property(x => x.PreparationBaseTargetSeconds).IsRequired();
            b.Property(x => x.ReadyTargetSeconds).IsRequired();
            b.Property(x => x.UpdatedAt).IsRequired();
        });

        modelBuilder.Entity<OrderItemRow>(b =>
        {
            b.ToTable("order_items");
            b.HasKey(x => x.Id);
            b.Property(x => x.TenantId).IsRequired();
            b.Property(x => x.OrderId).IsRequired();
            b.Property(x => x.SkuId).IsRequired();
            b.Property(x => x.SkuCode).IsRequired();
            b.Property(x => x.SkuName).IsRequired();
            b.Property(x => x.UnitPriceCents).IsRequired();
            b.Property(x => x.Quantity).IsRequired();
            b.Property(x => x.TotalCents).IsRequired();
            b.Property(x => x.CreatedAt).IsRequired();
            b.HasIndex(x => new { x.TenantId, x.OrderId });
            b.HasIndex(x => x.OrderId);
            b.HasOne<OrderRow>().WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PaymentRow>(b =>
        {
            b.ToTable("payments");
            b.HasKey(x => x.Id);
            b.Property(x => x.TenantId).IsRequired();
            b.Property(x => x.OrderId).IsRequired();
            b.Property(x => x.Method).HasConversion<int>().IsRequired();
            b.Property(x => x.Status).HasConversion<int>().IsRequired();
            b.Property(x => x.AmountCents).IsRequired();
            b.Property(x => x.Provider).IsRequired();
            b.Property(x => x.ProviderReference).IsRequired();
            b.Property(x => x.TransactionId).IsRequired();
            b.Property(x => x.PixPayload);
            b.Property(x => x.PixExpiresAt);
            b.Property(x => x.CreatedAt).IsRequired();
            b.Property(x => x.UpdatedAt).IsRequired();
            b.HasIndex(x => x.TenantId);
            b.HasIndex(x => new { x.TenantId, x.OrderId });
            b.HasIndex(x => x.OrderId);
            b.HasOne<OrderRow>().WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CartRow>(b =>
        {
            b.ToTable("carts");
            b.HasKey(x => x.Id);
            b.Property(x => x.TenantId).IsRequired();
            b.Property(x => x.CreatedAt).IsRequired();
            b.Property(x => x.UpdatedAt).IsRequired();
            b.HasIndex(x => x.TenantId);
            b.HasIndex(x => new { x.TenantId, x.UpdatedAt });
        });

        modelBuilder.Entity<CartItemRow>(b =>
        {
            b.ToTable("cart_items");
            b.HasKey(x => x.Id);
            b.Property(x => x.TenantId).IsRequired();
            b.Property(x => x.CartId).IsRequired();
            b.Property(x => x.SkuId).IsRequired();
            b.Property(x => x.Quantity).IsRequired();
            b.Property(x => x.CreatedAt).IsRequired();
            b.Property(x => x.UpdatedAt).IsRequired();
            b.HasIndex(x => new { x.TenantId, x.CartId });
            b.HasIndex(x => x.CartId);
            b.HasIndex(x => new { x.TenantId, x.CartId, x.SkuId }).IsUnique();
            b.HasOne<CartRow>().WithMany().HasForeignKey(x => x.CartId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CashRegisterShiftRow>(b =>
        {
            b.ToTable("cash_register_shifts");
            b.HasKey(x => x.Id);
            b.Property(x => x.TenantId).IsRequired();
            b.Property(x => x.Status).HasConversion<int>().IsRequired();
            b.Property(x => x.OpenedByUserId).IsRequired();
            b.Property(x => x.OpenedByEmail).IsRequired();
            b.Property(x => x.OpeningCashCents).IsRequired();
            b.Property(x => x.OpenedAt).IsRequired();
            b.Property(x => x.ClosedByUserId);
            b.Property(x => x.ClosedByEmail);
            b.Property(x => x.ClosingCashCents);
            b.Property(x => x.TotalSalesCents);
            b.Property(x => x.TotalCashSalesCents);
            b.Property(x => x.ExpectedCashCents);
            b.Property(x => x.ClosedAt);
            b.Property(x => x.CreatedAt).IsRequired();
            b.Property(x => x.UpdatedAt).IsRequired();
            b.HasIndex(x => x.TenantId);
            b.HasIndex(x => new { x.TenantId, x.Status });
            b.HasIndex(x => new { x.TenantId, x.OpenedAt });
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

public sealed class CategoryRow
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class SkuRow
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string CategoryCode { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string NormalizedCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int PriceCents { get; set; }
    public int? AveragePrepSeconds { get; set; }
    public string? ImageUrl { get; set; }
    public string? NfeCProd { get; set; }
    public string? NfeCEan { get; set; }
    public string? NfeCfop { get; set; }
    public string? NfeUCom { get; set; }
    public decimal? NfeQCom { get; set; }
    public decimal? NfeVUnCom { get; set; }
    public decimal? NfeVProd { get; set; }
    public string? NfeCEanTrib { get; set; }
    public string? NfeUTrib { get; set; }
    public decimal? NfeQTrib { get; set; }
    public decimal? NfeVUnTrib { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class OrderRow
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid? CartId { get; set; }
    public OrderFulfillment Fulfillment { get; set; }
    public int TotalCents { get; set; }
    public OrderStatus Status { get; set; }
    public OrderKitchenStatus KitchenStatus { get; set; }
    public string? Comanda { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? QueuedAt { get; set; }
    public DateTimeOffset? InPreparationAt { get; set; }
    public DateTimeOffset? ReadyAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
}

public sealed class KitchenSlaRow
{
    public Guid TenantId { get; set; }
    public int QueuedTargetSeconds { get; set; }
    public int PreparationBaseTargetSeconds { get; set; }
    public int ReadyTargetSeconds { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class OrderItemRow
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid OrderId { get; set; }
    public Guid SkuId { get; set; }
    public string SkuCode { get; set; } = string.Empty;
    public string SkuName { get; set; } = string.Empty;
    public int UnitPriceCents { get; set; }
    public int Quantity { get; set; }
    public int TotalCents { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class PaymentRow
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid OrderId { get; set; }
    public PaymentMethod Method { get; set; }
    public PaymentStatus Status { get; set; }
    public int AmountCents { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string ProviderReference { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public string? PixPayload { get; set; }
    public DateTimeOffset? PixExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class CartRow
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class CartItemRow
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid CartId { get; set; }
    public Guid SkuId { get; set; }
    public int Quantity { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class CashRegisterShiftRow
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public CashRegisterShiftStatus Status { get; set; }
    public Guid OpenedByUserId { get; set; }
    public string OpenedByEmail { get; set; } = string.Empty;
    public int OpeningCashCents { get; set; }
    public DateTimeOffset OpenedAt { get; set; }
    public Guid? ClosedByUserId { get; set; }
    public string? ClosedByEmail { get; set; }
    public int? ClosingCashCents { get; set; }
    public int? TotalSalesCents { get; set; }
    public int? TotalCashSalesCents { get; set; }
    public int? ExpectedCashCents { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
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
        new(
            row.Id,
            row.TenantId,
            row.CategoryCode,
            row.Code,
            row.Name,
            row.PriceCents,
            row.AveragePrepSeconds,
            row.ImageUrl,
            row.NfeCProd,
            row.NfeCEan,
            row.NfeCfop,
            row.NfeUCom,
            row.NfeQCom,
            row.NfeVUnCom,
            row.NfeVProd,
            row.NfeCEanTrib,
            row.NfeUTrib,
            row.NfeQTrib,
            row.NfeVUnTrib,
            row.IsActive,
            row.CreatedAt,
            row.UpdatedAt
        );

    public static string NormalizeCode(string code) => (code ?? string.Empty).Trim().ToUpperInvariant();
}

internal static class CategoryMapping
{
    public static Category ToDomain(this CategoryRow row) =>
        new(
            row.Id,
            row.TenantId,
            row.Code,
            row.Slug,
            row.Name,
            row.IsActive,
            row.CreatedAt,
            row.UpdatedAt
        );

    public static string NormalizeNumericCode(string code)
    {
        var raw = (code ?? string.Empty).Trim();
        if (raw.Length == 0) return string.Empty;
        if (!int.TryParse(raw, out var n)) return string.Empty;
        if (n <= 0) return string.Empty;
        return n.ToString("D5");
    }

    public static string NormalizeSlug(string code)
    {
        var raw = (code ?? string.Empty).Trim().ToLowerInvariant();
        if (raw.Length == 0) return string.Empty;

        var chars = raw.Select(ch => char.IsLetterOrDigit(ch) ? ch : '-').ToArray();
        var s = new string(chars);
        while (s.Contains("--", StringComparison.Ordinal)) s = s.Replace("--", "-", StringComparison.Ordinal);
        s = s.Trim('-');
        return s;
    }
}

internal static class OrderMapping
{
    public static Order ToDomain(this OrderRow row) =>
        new(
            row.Id,
            row.TenantId,
            row.CartId,
            row.Fulfillment,
            row.TotalCents,
            row.Status,
            row.KitchenStatus,
            row.Comanda,
            row.CreatedAt,
            row.UpdatedAt,
            row.QueuedAt,
            row.InPreparationAt,
            row.ReadyAt,
            row.CompletedAt,
            row.CancelledAt
        );
}

internal static class KitchenSlaMapping
{
    public static KitchenSla ToDomain(this KitchenSlaRow row) =>
        new(row.TenantId, row.QueuedTargetSeconds, row.PreparationBaseTargetSeconds, row.ReadyTargetSeconds, row.UpdatedAt);
}

internal static class OrderItemMapping
{
    public static OrderItem ToDomain(this OrderItemRow row) =>
        new(
            row.Id,
            row.TenantId,
            row.OrderId,
            row.SkuId,
            row.SkuCode,
            row.SkuName,
            row.UnitPriceCents,
            row.Quantity,
            row.TotalCents,
            row.CreatedAt
        );
}

internal static class PaymentMapping
{
    public static Payment ToDomain(this PaymentRow row) =>
        new(
            row.Id,
            row.TenantId,
            row.OrderId,
            row.Method,
            row.Status,
            row.AmountCents,
            row.Provider,
            row.ProviderReference,
            row.TransactionId,
            row.PixPayload,
            row.PixExpiresAt,
            row.CreatedAt,
            row.UpdatedAt
        );
}

internal static class CartMapping
{
    public static ShoppingCart ToDomain(this CartRow row) =>
        new(
            row.Id,
            row.TenantId,
            row.CreatedAt,
            row.UpdatedAt
        );
}

internal static class CartItemMapping
{
    public static ShoppingCartItem ToDomain(this CartItemRow row) =>
        new(
            row.Id,
            row.TenantId,
            row.CartId,
            row.SkuId,
            row.Quantity,
            row.CreatedAt,
            row.UpdatedAt
        );
}

internal static class CashRegisterShiftMapping
{
    public static CashRegisterShift ToDomain(this CashRegisterShiftRow row) =>
        new(
            row.Id,
            row.TenantId,
            row.Status,
            row.OpenedByUserId,
            row.OpenedByEmail,
            row.OpeningCashCents,
            row.OpenedAt,
            row.ClosedByUserId,
            row.ClosedByEmail,
            row.ClosingCashCents,
            row.TotalSalesCents,
            row.TotalCashSalesCents,
            row.ExpectedCashCents,
            row.ClosedAt,
            row.CreatedAt,
            row.UpdatedAt
        );
}
