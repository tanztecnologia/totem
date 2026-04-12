using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TotemAPI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _20260411000000_Cart : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "carts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_carts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cart_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CartId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SkuId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cart_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cart_items_carts_CartId",
                        column: x => x.CartId,
                        principalTable: "carts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cart_items_CartId",
                table: "cart_items",
                column: "CartId");

            migrationBuilder.CreateIndex(
                name: "IX_cart_items_TenantId_CartId",
                table: "cart_items",
                columns: new[] { "TenantId", "CartId" });

            migrationBuilder.CreateIndex(
                name: "IX_cart_items_TenantId_CartId_SkuId",
                table: "cart_items",
                columns: new[] { "TenantId", "CartId", "SkuId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_carts_TenantId",
                table: "carts",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_carts_TenantId_UpdatedAt",
                table: "carts",
                columns: new[] { "TenantId", "UpdatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cart_items");

            migrationBuilder.DropTable(
                name: "carts");
        }
    }
}
