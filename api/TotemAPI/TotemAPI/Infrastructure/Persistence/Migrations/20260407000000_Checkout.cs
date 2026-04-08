using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TotemAPI.Infrastructure.Persistence.Migrations;

[DbContext(typeof(TotemDbContext))]
[Migration("20260407000000_Checkout")]
public sealed partial class Checkout : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "orders",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                Fulfillment = table.Column<int>(type: "INTEGER", nullable: false),
                TotalCents = table.Column<int>(type: "INTEGER", nullable: false),
                Status = table.Column<int>(type: "INTEGER", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
            },
            constraints: table => { table.PrimaryKey("PK_orders", x => x.Id); }
        );

        migrationBuilder.CreateTable(
            name: "order_items",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                OrderId = table.Column<Guid>(type: "TEXT", nullable: false),
                SkuId = table.Column<Guid>(type: "TEXT", nullable: false),
                SkuCode = table.Column<string>(type: "TEXT", nullable: false),
                SkuName = table.Column<string>(type: "TEXT", nullable: false),
                UnitPriceCents = table.Column<int>(type: "INTEGER", nullable: false),
                Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                TotalCents = table.Column<int>(type: "INTEGER", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_order_items", x => x.Id);
                table.ForeignKey(
                    name: "FK_order_items_orders_OrderId",
                    column: x => x.OrderId,
                    principalTable: "orders",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "payments",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                OrderId = table.Column<Guid>(type: "TEXT", nullable: false),
                Method = table.Column<int>(type: "INTEGER", nullable: false),
                Status = table.Column<int>(type: "INTEGER", nullable: false),
                AmountCents = table.Column<int>(type: "INTEGER", nullable: false),
                Provider = table.Column<string>(type: "TEXT", nullable: false),
                ProviderReference = table.Column<string>(type: "TEXT", nullable: false),
                TransactionId = table.Column<string>(type: "TEXT", nullable: false),
                PixPayload = table.Column<string>(type: "TEXT", nullable: true),
                PixExpiresAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_payments", x => x.Id);
                table.ForeignKey(
                    name: "FK_payments_orders_OrderId",
                    column: x => x.OrderId,
                    principalTable: "orders",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateIndex(
            name: "IX_orders_TenantId",
            table: "orders",
            column: "TenantId"
        );

        migrationBuilder.CreateIndex(
            name: "IX_orders_TenantId_CreatedAt",
            table: "orders",
            columns: new[] { "TenantId", "CreatedAt" }
        );

        migrationBuilder.CreateIndex(
            name: "IX_order_items_OrderId",
            table: "order_items",
            column: "OrderId"
        );

        migrationBuilder.CreateIndex(
            name: "IX_order_items_TenantId_OrderId",
            table: "order_items",
            columns: new[] { "TenantId", "OrderId" }
        );

        migrationBuilder.CreateIndex(
            name: "IX_payments_OrderId",
            table: "payments",
            column: "OrderId"
        );

        migrationBuilder.CreateIndex(
            name: "IX_payments_TenantId",
            table: "payments",
            column: "TenantId"
        );

        migrationBuilder.CreateIndex(
            name: "IX_payments_TenantId_OrderId",
            table: "payments",
            columns: new[] { "TenantId", "OrderId" }
        );
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "payments");
        migrationBuilder.DropTable(name: "order_items");
        migrationBuilder.DropTable(name: "orders");
    }
}

