using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TotemAPI.Infrastructure.Persistence.Migrations;

[DbContext(typeof(TotemDbContext))]
[Migration("20260414000001_CategoryNumericCodeAndSlug")]
public sealed partial class CategoryNumericCodeAndSlug : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Slug",
            table: "categories",
            type: "TEXT",
            nullable: false,
            defaultValue: ""
        );

        migrationBuilder.Sql("UPDATE categories SET Slug = Code WHERE Slug = '' OR Slug IS NULL;");

        migrationBuilder.Sql(
            """
            WITH numbered AS (
              SELECT
                Id,
                printf('%05d', ROW_NUMBER() OVER (PARTITION BY TenantId ORDER BY CreatedAt, Id)) AS NewCode
              FROM categories
            )
            UPDATE categories
            SET Code = (SELECT NewCode FROM numbered WHERE numbered.Id = categories.Id);
            """
        );

        migrationBuilder.CreateIndex(
            name: "IX_categories_TenantId_Slug",
            table: "categories",
            columns: new[] { "TenantId", "Slug" },
            unique: true
        );
    }
}

