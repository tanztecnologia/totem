using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TotemAPI.Infrastructure.Persistence.Migrations;

[DbContext(typeof(TotemDbContext))]
[Migration("20260414000000_Categories")]
public sealed partial class Categories : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "categories",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                Code = table.Column<string>(type: "TEXT", nullable: false),
                Name = table.Column<string>(type: "TEXT", nullable: false),
                IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
            },
            constraints: table => { table.PrimaryKey("PK_categories", x => x.Id); }
        );

        migrationBuilder.CreateIndex(
            name: "IX_categories_TenantId",
            table: "categories",
            column: "TenantId"
        );

        migrationBuilder.CreateIndex(
            name: "IX_categories_TenantId_Code",
            table: "categories",
            columns: new[] { "TenantId", "Code" },
            unique: true
        );

        migrationBuilder.CreateIndex(
            name: "IX_categories_TenantId_IsActive",
            table: "categories",
            columns: new[] { "TenantId", "IsActive" }
        );
    }
}
