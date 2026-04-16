using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TotemAPI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveEmpresaXSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM cart_items
                WHERE TenantId IN (SELECT Id FROM tenants WHERE NormalizedName = 'EMPRESA X');

                DELETE FROM carts
                WHERE TenantId IN (SELECT Id FROM tenants WHERE NormalizedName = 'EMPRESA X');

                DELETE FROM order_items
                WHERE TenantId IN (SELECT Id FROM tenants WHERE NormalizedName = 'EMPRESA X');

                DELETE FROM payments
                WHERE TenantId IN (SELECT Id FROM tenants WHERE NormalizedName = 'EMPRESA X');

                DELETE FROM orders
                WHERE TenantId IN (SELECT Id FROM tenants WHERE NormalizedName = 'EMPRESA X');

                DELETE FROM cash_register_shifts
                WHERE TenantId IN (SELECT Id FROM tenants WHERE NormalizedName = 'EMPRESA X');

                DELETE FROM sku_stock_consumptions
                WHERE TenantId IN (SELECT Id FROM tenants WHERE NormalizedName = 'EMPRESA X');

                DELETE FROM sku_stock_ledger
                WHERE TenantId IN (SELECT Id FROM tenants WHERE NormalizedName = 'EMPRESA X');

                DELETE FROM categories
                WHERE TenantId IN (SELECT Id FROM tenants WHERE NormalizedName = 'EMPRESA X');

                DELETE FROM skus
                WHERE TenantId IN (SELECT Id FROM tenants WHERE NormalizedName = 'EMPRESA X');

                DELETE FROM kitchen_sla
                WHERE TenantId IN (SELECT Id FROM tenants WHERE NormalizedName = 'EMPRESA X');

                DELETE FROM users
                WHERE TenantId IN (SELECT Id FROM tenants WHERE NormalizedName = 'EMPRESA X');

                DELETE FROM tenants
                WHERE NormalizedName = 'EMPRESA X';
                """
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
