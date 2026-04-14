using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TotemAPI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCashRegisterShifts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cash_register_shifts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    OpenedByUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OpenedByEmail = table.Column<string>(type: "TEXT", nullable: false),
                    OpeningCashCents = table.Column<int>(type: "INTEGER", nullable: false),
                    OpenedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ClosedByUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ClosedByEmail = table.Column<string>(type: "TEXT", nullable: true),
                    ClosingCashCents = table.Column<int>(type: "INTEGER", nullable: true),
                    TotalSalesCents = table.Column<int>(type: "INTEGER", nullable: true),
                    TotalCashSalesCents = table.Column<int>(type: "INTEGER", nullable: true),
                    ExpectedCashCents = table.Column<int>(type: "INTEGER", nullable: true),
                    ClosedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cash_register_shifts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cash_register_shifts_TenantId",
                table: "cash_register_shifts",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_cash_register_shifts_TenantId_OpenedAt",
                table: "cash_register_shifts",
                columns: new[] { "TenantId", "OpenedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_cash_register_shifts_TenantId_Status",
                table: "cash_register_shifts",
                columns: new[] { "TenantId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cash_register_shifts");
        }
    }
}
