using Microsoft.EntityFrameworkCore;
using TotemAPI.Features.Catalog.Domain;
using TotemAPI.Features.Identity.Application.Abstractions;
using TotemAPI.Features.Identity.Domain;

namespace TotemAPI.Infrastructure.Persistence;

public static class TanzMySqlSeeder
{
    public static async Task SeedAsync(TotemDbContext db, IPasswordHasher passwordHasher, CancellationToken ct)
    {
        var tenantName = "TANZ";
        var tenantNormalizedName = tenantName.Trim().ToUpperInvariant();
        var tenant = await db.Tenants.FirstOrDefaultAsync(x => x.NormalizedName == tenantNormalizedName, ct);

        if (tenant is null)
        {
            tenant = new TenantRow
            {
                Id = Guid.Parse("7c0b9c35-2fcb-4af0-82cc-6868ea6f3bf0"),
                Name = tenantName,
                NormalizedName = tenantNormalizedName,
                CreatedAt = DateTimeOffset.UtcNow
            };
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync(ct);
        }

        await EnsureUserAsync(
            db,
            passwordHasher,
            tenant.Id,
            Guid.Parse("9c0e8ac6-3b50-41d7-a5d5-6b5d61b2de0a"),
            "admin@tanz.local",
            UserRole.Admin,
            ct
        );
        await EnsureUserAsync(
            db,
            passwordHasher,
            tenant.Id,
            Guid.Parse("14fd8f8a-0fd4-4d63-8d7c-2ab585d110c2"),
            "staff@tanz.local",
            UserRole.Staff,
            ct
        );
        await EnsureUserAsync(
            db,
            passwordHasher,
            tenant.Id,
            Guid.Parse("b48bdb0f-317d-4b3b-88e2-7d6f6c9cc8a7"),
            "kitchen@tanz.local",
            UserRole.Staff,
            ct
        );
        await EnsureUserAsync(
            db,
            passwordHasher,
            tenant.Id,
            Guid.Parse("a9d5e5aa-6da8-4adf-9b32-35a0b8a8f51a"),
            "pdv@tanz.local",
            UserRole.Pdv,
            ct
        );
        await EnsureUserAsync(
            db,
            passwordHasher,
            tenant.Id,
            Guid.Parse("6f6d64a5-1d6f-4c3a-8f90-7e245c3bd79a"),
            "waiter@tanz.local",
            UserRole.Waiter,
            ct
        );
        await EnsureUserAsync(
            db,
            passwordHasher,
            tenant.Id,
            Guid.Parse("4b20cb61-70b2-4d8f-8fb1-53e0c2d2db5f"),
            "totem@tanz.local",
            UserRole.Totem,
            ct
        );

        var now = DateTimeOffset.UtcNow;

        await EnsureCategoryAsync(db, tenant.Id, Guid.Parse("3b1a0c6e-f0a3-4c74-8c6b-e5dc0d8a9f77"), "00001", "hamburgers", "Hambúrgueres", now, ct);
        await EnsureCategoryAsync(db, tenant.Id, Guid.Parse("c7b69d4e-1f1b-45f1-bb9a-72c4c8b44c1f"), "00002", "acompanhamentos", "Acompanhamentos", now, ct);
        await EnsureCategoryAsync(db, tenant.Id, Guid.Parse("0fefaa50-4d5a-4c48-a238-9ed5311f96d2"), "00003", "bebidas", "Bebidas", now, ct);
        await EnsureCategoryAsync(db, tenant.Id, Guid.Parse("dd0c055d-0b78-4d8b-9ec7-3c4e5e4a4c0a"), "00004", "sobremesas", "Sobremesas", now, ct);
        await EnsureCategoryAsync(db, tenant.Id, Guid.Parse("1c3d2ea8-6a26-4f14-987f-0c9f4fd1b98c"), "00005", "insumos", "Insumos", now, ct);

        await EnsureSkuAsync(
            db,
            new SkuRow
            {
                Id = Guid.Parse("b0701f66-512d-4c3d-9b1f-9877a6b8c5a1"),
                TenantId = tenant.Id,
                CategoryCode = "00001",
                Code = "TZ-BURGER-CLASSIC",
                NormalizedCode = "TZ-BURGER-CLASSIC",
                Name = "TZ Burger Classic",
                PriceCents = 2890,
                AveragePrepSeconds = 480,
                TracksStock = false,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            ct
        );
        await EnsureSkuAsync(
            db,
            new SkuRow
            {
                Id = Guid.Parse("c1c2c30b-7f0f-4ff0-9c42-589d07d3a4c7"),
                TenantId = tenant.Id,
                CategoryCode = "00002",
                Code = "BATATA-P",
                NormalizedCode = "BATATA-P",
                Name = "Batata Frita P",
                PriceCents = 1290,
                AveragePrepSeconds = 300,
                TracksStock = false,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            ct
        );
        await EnsureSkuAsync(
            db,
            new SkuRow
            {
                Id = Guid.Parse("9b1a1f39-52a0-476f-a0fd-3d2d4fb0c070"),
                TenantId = tenant.Id,
                CategoryCode = "00003",
                Code = "COCA-350",
                NormalizedCode = "COCA-350",
                Name = "Coca-Cola Lata 350ml",
                PriceCents = 890,
                TracksStock = true,
                StockBaseUnit = StockBaseUnit.Unit,
                StockOnHandBaseQty = 60,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            ct
        );
        await EnsureSkuAsync(
            db,
            new SkuRow
            {
                Id = Guid.Parse("a1ab3dfe-0d08-4a5c-9c1c-6c00a3b5a9e1"),
                TenantId = tenant.Id,
                CategoryCode = "00003",
                Code = "AGUA-500",
                NormalizedCode = "AGUA-500",
                Name = "Água Mineral 500ml",
                PriceCents = 590,
                TracksStock = true,
                StockBaseUnit = StockBaseUnit.Unit,
                StockOnHandBaseQty = 40,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            ct
        );
        await EnsureSkuAsync(
            db,
            new SkuRow
            {
                Id = Guid.Parse("3a3d4d92-f3b2-4b3c-a96c-c08a3f44b563"),
                TenantId = tenant.Id,
                CategoryCode = "00004",
                Code = "TZ-SUNDAE",
                NormalizedCode = "TZ-SUNDAE",
                Name = "Sundae Chocolate",
                PriceCents = 1090,
                AveragePrepSeconds = 120,
                TracksStock = false,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            ct
        );
        await EnsureSkuAsync(
            db,
            new SkuRow
            {
                Id = Guid.Parse("39b1b3aa-cd5c-48a0-9cf7-9f0bdbb7e0b7"),
                TenantId = tenant.Id,
                CategoryCode = "00001",
                Code = "TZ-COMBO-CLASSIC",
                NormalizedCode = "TZ-COMBO-CLASSIC",
                Name = "Combo Classic (Burger + Batata P + Bebida)",
                PriceCents = 4490,
                AveragePrepSeconds = 480,
                TracksStock = false,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            ct
        );

        await EnsureSkuAsync(
            db,
            new SkuRow
            {
                Id = Guid.Parse("d2b2b4a8-6fd3-48cc-9ccf-97de0fb3b8aa"),
                TenantId = tenant.Id,
                CategoryCode = "00005",
                Code = "INS-BATATA-CRUA",
                NormalizedCode = "INS-BATATA-CRUA",
                Name = "Batata in natura (g)",
                PriceCents = 0,
                TracksStock = true,
                StockBaseUnit = StockBaseUnit.Gram,
                StockOnHandBaseQty = 25000,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            ct
        );
        await EnsureSkuAsync(
            db,
            new SkuRow
            {
                Id = Guid.Parse("1b8b2b15-9a54-4d45-9cb2-2a5bd5d59df6"),
                TenantId = tenant.Id,
                CategoryCode = "00005",
                Code = "INS-CARNE-BOI",
                NormalizedCode = "INS-CARNE-BOI",
                Name = "Carne bovina moída (g)",
                PriceCents = 0,
                TracksStock = true,
                StockBaseUnit = StockBaseUnit.Gram,
                StockOnHandBaseQty = 12000,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            ct
        );
        await EnsureSkuAsync(
            db,
            new SkuRow
            {
                Id = Guid.Parse("3d1a3e6c-9c9c-4d67-8c1d-63d58d4e8f0d"),
                TenantId = tenant.Id,
                CategoryCode = "00005",
                Code = "INS-PAO-HAMB",
                NormalizedCode = "INS-PAO-HAMB",
                Name = "Pão de hambúrguer (un)",
                PriceCents = 0,
                TracksStock = true,
                StockBaseUnit = StockBaseUnit.Unit,
                StockOnHandBaseQty = 100,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            ct
        );
        await EnsureSkuAsync(
            db,
            new SkuRow
            {
                Id = Guid.Parse("5b6e7c0c-8b2f-4d35-a4b6-1c6c0d7b9c2e"),
                TenantId = tenant.Id,
                CategoryCode = "00005",
                Code = "INS-QUEIJO",
                NormalizedCode = "INS-QUEIJO",
                Name = "Queijo fatiado (un)",
                PriceCents = 0,
                TracksStock = true,
                StockBaseUnit = StockBaseUnit.Unit,
                StockOnHandBaseQty = 100,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            ct
        );

        await EnsureStockConsumptionAsync(
            db,
            new SkuStockConsumptionRow
            {
                Id = Guid.Parse("1b9fe6ef-3f15-4f4c-9aa3-8511f8b0c2c3"),
                TenantId = tenant.Id,
                SkuId = Guid.Parse("c1c2c30b-7f0f-4ff0-9c42-589d07d3a4c7"),
                SourceSkuId = Guid.Parse("d2b2b4a8-6fd3-48cc-9ccf-97de0fb3b8aa"),
                QuantityBase = 150
            },
            ct
        );
        await EnsureStockConsumptionAsync(
            db,
            new SkuStockConsumptionRow
            {
                Id = Guid.Parse("8fb6c5d0-4f9c-4c7d-98b0-70e4f3e7b0e1"),
                TenantId = tenant.Id,
                SkuId = Guid.Parse("b0701f66-512d-4c3d-9b1f-9877a6b8c5a1"),
                SourceSkuId = Guid.Parse("3d1a3e6c-9c9c-4d67-8c1d-63d58d4e8f0d"),
                QuantityBase = 1
            },
            ct
        );
        await EnsureStockConsumptionAsync(
            db,
            new SkuStockConsumptionRow
            {
                Id = Guid.Parse("d3d2d9a1-8d5e-4a4b-8f2c-4b5c6d7e8f01"),
                TenantId = tenant.Id,
                SkuId = Guid.Parse("b0701f66-512d-4c3d-9b1f-9877a6b8c5a1"),
                SourceSkuId = Guid.Parse("1b8b2b15-9a54-4d45-9cb2-2a5bd5d59df6"),
                QuantityBase = 160
            },
            ct
        );
        await EnsureStockConsumptionAsync(
            db,
            new SkuStockConsumptionRow
            {
                Id = Guid.Parse("8b2f7c21-59ce-4d38-9c2f-9fe1c1c85ac7"),
                TenantId = tenant.Id,
                SkuId = Guid.Parse("b0701f66-512d-4c3d-9b1f-9877a6b8c5a1"),
                SourceSkuId = Guid.Parse("5b6e7c0c-8b2f-4d35-a4b6-1c6c0d7b9c2e"),
                QuantityBase = 1
            },
            ct
        );

        await EnsureKitchenSlaAsync(db, tenant.Id, now, ct);

        var admin = await db.Users.FirstAsync(x => x.TenantId == tenant.Id && x.Role == UserRole.Admin, ct);
        await EnsureInitialStockLedgerAsync(db, tenant.Id, admin.Id, ct);
    }

    private static async Task EnsureUserAsync(
        TotemDbContext db,
        IPasswordHasher passwordHasher,
        Guid tenantId,
        Guid userId,
        string email,
        UserRole role,
        CancellationToken ct
    )
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var exists = await db.Users.AnyAsync(x => x.TenantId == tenantId && x.NormalizedEmail == normalizedEmail, ct);
        if (exists) return;

        db.Users.Add(
            new UserRow
            {
                Id = userId,
                TenantId = tenantId,
                Email = email,
                NormalizedEmail = normalizedEmail,
                PasswordHash = passwordHasher.Hash("123456"),
                Role = role,
                CreatedAt = DateTimeOffset.UtcNow
            }
        );
        await db.SaveChangesAsync(ct);
    }

    private static async Task EnsureCategoryAsync(
        TotemDbContext db,
        Guid tenantId,
        Guid id,
        string code,
        string slug,
        string name,
        DateTimeOffset now,
        CancellationToken ct
    )
    {
        var exists = await db.Categories.AnyAsync(x => x.TenantId == tenantId && x.Code == code, ct);
        if (exists) return;

        db.Categories.Add(
            new CategoryRow
            {
                Id = id,
                TenantId = tenantId,
                Code = code,
                Slug = slug,
                Name = name,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            }
        );
        await db.SaveChangesAsync(ct);
    }

    private static async Task EnsureSkuAsync(TotemDbContext db, SkuRow sku, CancellationToken ct)
    {
        var exists = await db.Skus.AnyAsync(x => x.TenantId == sku.TenantId && x.NormalizedCode == sku.NormalizedCode, ct);
        if (exists) return;

        db.Skus.Add(sku);
        await db.SaveChangesAsync(ct);
    }

    private static async Task EnsureStockConsumptionAsync(TotemDbContext db, SkuStockConsumptionRow row, CancellationToken ct)
    {
        var exists = await db.SkuStockConsumptions.AnyAsync(
            x => x.TenantId == row.TenantId && x.SkuId == row.SkuId && x.SourceSkuId == row.SourceSkuId,
            ct
        );
        if (exists) return;

        db.SkuStockConsumptions.Add(row);
        await db.SaveChangesAsync(ct);
    }

    private static async Task EnsureKitchenSlaAsync(TotemDbContext db, Guid tenantId, DateTimeOffset now, CancellationToken ct)
    {
        var exists = await db.KitchenSlas.AnyAsync(x => x.TenantId == tenantId, ct);
        if (exists) return;

        db.KitchenSlas.Add(
            new KitchenSlaRow
            {
                TenantId = tenantId,
                QueuedTargetSeconds = 120,
                PreparationBaseTargetSeconds = 480,
                ReadyTargetSeconds = 120,
                UpdatedAt = now
            }
        );
        await db.SaveChangesAsync(ct);
    }

    private static async Task EnsureInitialStockLedgerAsync(
        TotemDbContext db,
        Guid tenantId,
        Guid adminUserId,
        CancellationToken ct
    )
    {
        var now = DateTimeOffset.UtcNow;
        await EnsureLedgerAsync(
            db,
            new SkuStockLedgerRow
            {
                Id = Guid.Parse("c2c0f5fb-9e2a-4dcb-90b0-3f21a2b0f9e1"),
                TenantId = tenantId,
                SkuId = Guid.Parse("9b1a1f39-52a0-476f-a0fd-3d2d4fb0c070"),
                DeltaBaseQty = 60,
                StockAfterBaseQty = 60,
                OriginType = StockLedgerOriginType.InitialStock,
                Notes = "seed",
                ActorUserId = adminUserId,
                CreatedAt = now
            },
            ct
        );
        await EnsureLedgerAsync(
            db,
            new SkuStockLedgerRow
            {
                Id = Guid.Parse("5f0a6c1a-3f5a-4c0d-9b1c-4e9f1a2b3c4d"),
                TenantId = tenantId,
                SkuId = Guid.Parse("a1ab3dfe-0d08-4a5c-9c1c-6c00a3b5a9e1"),
                DeltaBaseQty = 40,
                StockAfterBaseQty = 40,
                OriginType = StockLedgerOriginType.InitialStock,
                Notes = "seed",
                ActorUserId = adminUserId,
                CreatedAt = now
            },
            ct
        );
        await EnsureLedgerAsync(
            db,
            new SkuStockLedgerRow
            {
                Id = Guid.Parse("6c9b0c2d-1e3f-4a5b-9c7d-2e1f0a9b8c7d"),
                TenantId = tenantId,
                SkuId = Guid.Parse("d2b2b4a8-6fd3-48cc-9ccf-97de0fb3b8aa"),
                DeltaBaseQty = 25000,
                StockAfterBaseQty = 25000,
                OriginType = StockLedgerOriginType.InitialStock,
                Notes = "seed",
                ActorUserId = adminUserId,
                CreatedAt = now
            },
            ct
        );
        await EnsureLedgerAsync(
            db,
            new SkuStockLedgerRow
            {
                Id = Guid.Parse("b2d9f0a1-2c3d-4e5f-8a9b-0c1d2e3f4a5b"),
                TenantId = tenantId,
                SkuId = Guid.Parse("1b8b2b15-9a54-4d45-9cb2-2a5bd5d59df6"),
                DeltaBaseQty = 12000,
                StockAfterBaseQty = 12000,
                OriginType = StockLedgerOriginType.InitialStock,
                Notes = "seed",
                ActorUserId = adminUserId,
                CreatedAt = now
            },
            ct
        );
        await EnsureLedgerAsync(
            db,
            new SkuStockLedgerRow
            {
                Id = Guid.Parse("9a8b7c6d-5e4f-3a2b-1c0d-9e8f7a6b5c4d"),
                TenantId = tenantId,
                SkuId = Guid.Parse("3d1a3e6c-9c9c-4d67-8c1d-63d58d4e8f0d"),
                DeltaBaseQty = 100,
                StockAfterBaseQty = 100,
                OriginType = StockLedgerOriginType.InitialStock,
                Notes = "seed",
                ActorUserId = adminUserId,
                CreatedAt = now
            },
            ct
        );
        await EnsureLedgerAsync(
            db,
            new SkuStockLedgerRow
            {
                Id = Guid.Parse("0a1b2c3d-4e5f-6071-8293-a4b5c6d7e8f9"),
                TenantId = tenantId,
                SkuId = Guid.Parse("5b6e7c0c-8b2f-4d35-a4b6-1c6c0d7b9c2e"),
                DeltaBaseQty = 100,
                StockAfterBaseQty = 100,
                OriginType = StockLedgerOriginType.InitialStock,
                Notes = "seed",
                ActorUserId = adminUserId,
                CreatedAt = now
            },
            ct
        );
    }

    private static async Task EnsureLedgerAsync(TotemDbContext db, SkuStockLedgerRow row, CancellationToken ct)
    {
        var exists = await db.SkuStockLedger.AnyAsync(x => x.TenantId == row.TenantId && x.Id == row.Id, ct);
        if (exists) return;

        db.SkuStockLedger.Add(row);
        await db.SaveChangesAsync(ct);
    }
}

