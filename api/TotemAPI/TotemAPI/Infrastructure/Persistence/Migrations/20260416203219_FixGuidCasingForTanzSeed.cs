using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TotemAPI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixGuidCasingForTanzSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            const string tanzLower = "7c0b9c35-2fcb-4af0-82cc-6868ea6f3bf0";
            const string tanzUpper = "7C0B9C35-2FCB-4AF0-82CC-6868EA6F3BF0";

            migrationBuilder.Sql(
                $"""
                UPDATE tenants
                SET Id = upper(Id)
                WHERE NormalizedName = 'TANZ';

                UPDATE users
                SET TenantId = upper(TenantId),
                    Id = upper(Id)
                WHERE TenantId = '{tanzLower}' OR TenantId = '{tanzUpper}';

                UPDATE categories
                SET TenantId = upper(TenantId),
                    Id = upper(Id)
                WHERE TenantId = '{tanzLower}' OR TenantId = '{tanzUpper}';

                UPDATE skus
                SET TenantId = upper(TenantId),
                    Id = upper(Id)
                WHERE TenantId = '{tanzLower}' OR TenantId = '{tanzUpper}';

                UPDATE sku_stock_consumptions
                SET TenantId = upper(TenantId),
                    Id = upper(Id),
                    SkuId = upper(SkuId),
                    SourceSkuId = upper(SourceSkuId)
                WHERE TenantId = '{tanzLower}' OR TenantId = '{tanzUpper}';

                UPDATE sku_stock_ledger
                SET TenantId = upper(TenantId),
                    Id = upper(Id),
                    SkuId = upper(SkuId),
                    ActorUserId = upper(ActorUserId),
                    OriginId = CASE WHEN OriginId IS NULL THEN NULL ELSE upper(OriginId) END
                WHERE TenantId = '{tanzLower}' OR TenantId = '{tanzUpper}';

                UPDATE kitchen_sla
                SET TenantId = upper(TenantId)
                WHERE TenantId = '{tanzLower}' OR TenantId = '{tanzUpper}';
                """
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
