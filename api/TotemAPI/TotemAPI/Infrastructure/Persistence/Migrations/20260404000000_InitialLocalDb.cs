using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TotemAPI.Infrastructure.Persistence.Migrations;

[DbContext(typeof(TotemDbContext))]
[Migration("20260404000000_InitialLocalDb")]
public sealed partial class InitialLocalDb : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "tenants",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Name = table.Column<string>(type: "TEXT", nullable: false),
                NormalizedName = table.Column<string>(type: "TEXT", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
            },
            constraints: table => { table.PrimaryKey("PK_tenants", x => x.Id); }
        );

        migrationBuilder.CreateTable(
            name: "users",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                Email = table.Column<string>(type: "TEXT", nullable: false),
                NormalizedEmail = table.Column<string>(type: "TEXT", nullable: false),
                PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
                Role = table.Column<int>(type: "INTEGER", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
            },
            constraints: table => { table.PrimaryKey("PK_users", x => x.Id); }
        );

        migrationBuilder.CreateTable(
            name: "skus",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                Code = table.Column<string>(type: "TEXT", nullable: false),
                NormalizedCode = table.Column<string>(type: "TEXT", nullable: false),
                Name = table.Column<string>(type: "TEXT", nullable: false),
                PriceCents = table.Column<int>(type: "INTEGER", nullable: false),
                ImageUrl = table.Column<string>(type: "TEXT", nullable: true),
                IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
            },
            constraints: table => { table.PrimaryKey("PK_skus", x => x.Id); }
        );

        migrationBuilder.CreateIndex(
            name: "IX_tenants_NormalizedName",
            table: "tenants",
            column: "NormalizedName",
            unique: true
        );

        migrationBuilder.CreateIndex(
            name: "IX_users_NormalizedEmail",
            table: "users",
            column: "NormalizedEmail",
            unique: true
        );

        migrationBuilder.CreateIndex(
            name: "IX_users_TenantId_NormalizedEmail",
            table: "users",
            columns: new[] { "TenantId", "NormalizedEmail" },
            unique: true
        );

        migrationBuilder.CreateIndex(
            name: "IX_skus_TenantId",
            table: "skus",
            column: "TenantId"
        );

        migrationBuilder.CreateIndex(
            name: "IX_skus_TenantId_NormalizedCode",
            table: "skus",
            columns: new[] { "TenantId", "NormalizedCode" },
            unique: true
        );

        var tenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var totemUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var createdAt = new DateTimeOffset(2026, 04, 04, 0, 0, 0, TimeSpan.Zero);

        var passwordHash = HashPasswordDeterministic("123456");

        var createdAtIso = createdAt.ToString("O");
        var updatedAtIso = createdAtIso;

        migrationBuilder.Sql(
            $"INSERT INTO tenants (Id, Name, NormalizedName, CreatedAt) VALUES ('{tenantId:D}', 'Empresa X', 'EMPRESA X', '{createdAtIso}');"
        );

        migrationBuilder.Sql(
            $"INSERT INTO users (Id, TenantId, Email, NormalizedEmail, PasswordHash, Role, CreatedAt) VALUES ('{totemUserId:D}', '{tenantId:D}', 'totem@empresax.local', 'totem@empresax.local', '{passwordHash}', 3, '{createdAtIso}');"
        );

        migrationBuilder.Sql(
            $"INSERT INTO skus (Id, TenantId, Code, NormalizedCode, Name, PriceCents, ImageUrl, IsActive, CreatedAt, UpdatedAt) VALUES ('33333333-3333-3333-3333-333333333333', '{tenantId:D}', 'X-BURGER', 'X-BURGER', 'X Burger', 2500, NULL, 1, '{createdAtIso}', '{updatedAtIso}');"
        );

        migrationBuilder.Sql(
            $"INSERT INTO skus (Id, TenantId, Code, NormalizedCode, Name, PriceCents, ImageUrl, IsActive, CreatedAt, UpdatedAt) VALUES ('44444444-4444-4444-4444-444444444444', '{tenantId:D}', 'BATATA-P', 'BATATA-P', 'Batata Pequena', 1200, NULL, 1, '{createdAtIso}', '{updatedAtIso}');"
        );

        migrationBuilder.Sql(
            $"INSERT INTO skus (Id, TenantId, Code, NormalizedCode, Name, PriceCents, ImageUrl, IsActive, CreatedAt, UpdatedAt) VALUES ('55555555-5555-5555-5555-555555555555', '{tenantId:D}', 'COCA-350', 'COCA-350', 'Coca-Cola 350ml', 800, NULL, 1, '{createdAtIso}', '{updatedAtIso}');"
        );
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "skus");
        migrationBuilder.DropTable(name: "users");
        migrationBuilder.DropTable(name: "tenants");
    }

    private static string HashPasswordDeterministic(string password)
    {
        var salt = new byte[]
        {
            1, 2, 3, 4, 5, 6, 7, 8,
            9, 10, 11, 12, 13, 14, 15, 16
        };

        const int iterations = 100_000;
        const int keySize = 32;

        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password: password,
            salt: salt,
            iterations: iterations,
            hashAlgorithm: HashAlgorithmName.SHA256,
            outputLength: keySize
        );

        return $"v1.{iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }
}
