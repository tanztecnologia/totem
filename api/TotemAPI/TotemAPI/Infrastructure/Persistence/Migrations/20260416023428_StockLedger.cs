using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TotemAPI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class StockLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sku_stock_ledger",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SkuId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DeltaBaseQty = table.Column<decimal>(type: "TEXT", nullable: false),
                    StockAfterBaseQty = table.Column<decimal>(type: "TEXT", nullable: false),
                    OriginType = table.Column<int>(type: "INTEGER", nullable: false),
                    OriginId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    ActorUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sku_stock_ledger", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_sku_stock_ledger_TenantId_SkuId",
                table: "sku_stock_ledger",
                columns: new[] { "TenantId", "SkuId" });

            migrationBuilder.CreateIndex(
                name: "IX_sku_stock_ledger_TenantId_SkuId_CreatedAt",
                table: "sku_stock_ledger",
                columns: new[] { "TenantId", "SkuId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sku_stock_ledger");
        }
    }
}
