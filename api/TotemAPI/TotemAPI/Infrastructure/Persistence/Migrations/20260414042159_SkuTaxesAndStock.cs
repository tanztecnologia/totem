using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TotemAPI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SkuTaxesAndStock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NfeCofinsCst",
                table: "skus",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "NfeCofinsPCofins",
                table: "skus",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "NfeCofinsVBc",
                table: "skus",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "NfeCofinsVCofins",
                table: "skus",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NfeIcmsCst",
                table: "skus",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NfeIcmsModBc",
                table: "skus",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NfeIcmsOrig",
                table: "skus",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "NfeIcmsPIcms",
                table: "skus",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "NfeIcmsVBc",
                table: "skus",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "NfeIcmsVIcms",
                table: "skus",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NfePisCst",
                table: "skus",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "NfePisPPis",
                table: "skus",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "NfePisVBc",
                table: "skus",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "NfePisVPis",
                table: "skus",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StockBaseUnit",
                table: "skus",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "StockOnHandBaseQty",
                table: "skus",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "sku_stock_consumptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SkuId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SourceSkuId = table.Column<Guid>(type: "TEXT", nullable: false),
                    QuantityBase = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sku_stock_consumptions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_sku_stock_consumptions_TenantId",
                table: "sku_stock_consumptions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_sku_stock_consumptions_TenantId_SkuId",
                table: "sku_stock_consumptions",
                columns: new[] { "TenantId", "SkuId" });

            migrationBuilder.CreateIndex(
                name: "IX_sku_stock_consumptions_TenantId_SkuId_SourceSkuId",
                table: "sku_stock_consumptions",
                columns: new[] { "TenantId", "SkuId", "SourceSkuId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sku_stock_consumptions");

            migrationBuilder.DropColumn(
                name: "NfeCofinsCst",
                table: "skus");

            migrationBuilder.DropColumn(
                name: "NfeCofinsPCofins",
                table: "skus");

            migrationBuilder.DropColumn(
                name: "NfeCofinsVBc",
                table: "skus");

            migrationBuilder.DropColumn(
                name: "NfeCofinsVCofins",
                table: "skus");

            migrationBuilder.DropColumn(
                name: "NfeIcmsCst",
                table: "skus");

            migrationBuilder.DropColumn(
                name: "NfeIcmsModBc",
                table: "skus");

            migrationBuilder.DropColumn(
                name: "NfeIcmsOrig",
                table: "skus");

            migrationBuilder.DropColumn(
                name: "NfeIcmsPIcms",
                table: "skus");

            migrationBuilder.DropColumn(
                name: "NfeIcmsVBc",
                table: "skus");

            migrationBuilder.DropColumn(
                name: "NfeIcmsVIcms",
                table: "skus");

            migrationBuilder.DropColumn(
                name: "NfePisCst",
                table: "skus");

            migrationBuilder.DropColumn(
                name: "NfePisPPis",
                table: "skus");

            migrationBuilder.DropColumn(
                name: "NfePisVBc",
                table: "skus");

            migrationBuilder.DropColumn(
                name: "NfePisVPis",
                table: "skus");

            migrationBuilder.DropColumn(
                name: "StockBaseUnit",
                table: "skus");

            migrationBuilder.DropColumn(
                name: "StockOnHandBaseQty",
                table: "skus");
        }
    }
}
