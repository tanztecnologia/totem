using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TotemAPI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _20260411000002_OrderKitchenStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "KitchenStatus",
                table: "orders",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "orders",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.CreateIndex(
                name: "IX_orders_TenantId_UpdatedAt",
                table: "orders",
                columns: new[] { "TenantId", "UpdatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_orders_TenantId_UpdatedAt",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "KitchenStatus",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "orders");
        }
    }
}
