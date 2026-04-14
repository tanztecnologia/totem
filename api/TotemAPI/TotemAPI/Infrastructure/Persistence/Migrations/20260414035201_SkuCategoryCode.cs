using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TotemAPI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SkuCategoryCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CategoryId",
                table: "skus",
                newName: "CategoryCode");

            migrationBuilder.RenameIndex(
                name: "IX_skus_TenantId_CategoryId",
                table: "skus",
                newName: "IX_skus_TenantId_CategoryCode");

            migrationBuilder.Sql(
                """
                UPDATE skus
                SET CategoryCode = (
                    SELECT Code
                    FROM categories
                    WHERE categories.Id = skus.CategoryCode
                      AND categories.TenantId = skus.TenantId
                )
                WHERE EXISTS (
                    SELECT 1
                    FROM categories
                    WHERE categories.Id = skus.CategoryCode
                      AND categories.TenantId = skus.TenantId
                );
                """
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CategoryCode",
                table: "skus",
                newName: "CategoryId");

            migrationBuilder.RenameIndex(
                name: "IX_skus_TenantId_CategoryCode",
                table: "skus",
                newName: "IX_skus_TenantId_CategoryId");
        }
    }
}
