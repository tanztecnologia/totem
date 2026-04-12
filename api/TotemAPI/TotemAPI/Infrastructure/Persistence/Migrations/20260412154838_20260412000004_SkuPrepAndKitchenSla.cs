using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TotemAPI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _20260412000004_SkuPrepAndKitchenSla : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AveragePrepSeconds",
                table: "skus",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "kitchen_sla",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    QueuedTargetSeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    PreparationBaseTargetSeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    ReadyTargetSeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_kitchen_sla", x => x.TenantId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "kitchen_sla");

            migrationBuilder.DropColumn(
                name: "AveragePrepSeconds",
                table: "skus");
        }
    }
}
