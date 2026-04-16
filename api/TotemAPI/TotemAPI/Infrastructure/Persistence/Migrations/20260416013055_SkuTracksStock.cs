using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TotemAPI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SkuTracksStock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "TracksStock",
                table: "skus",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(
                """
                UPDATE skus
                SET TracksStock = 1
                WHERE StockBaseUnit IS NOT NULL OR StockOnHandBaseQty IS NOT NULL
                """
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TracksStock",
                table: "skus");
        }
    }
}
