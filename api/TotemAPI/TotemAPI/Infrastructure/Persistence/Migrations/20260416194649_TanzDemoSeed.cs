using System;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TotemAPI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TanzDemoSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var tenantId = Guid.Parse("7c0b9c35-2fcb-4af0-82cc-6868ea6f3bf0");
            var adminUserId = Guid.Parse("9c0e8ac6-3b50-41d7-a5d5-6b5d61b2de0a");
            var staffUserId = Guid.Parse("14fd8f8a-0fd4-4d63-8d7c-2ab585d110c2");
            var kitchenUserId = Guid.Parse("b48bdb0f-317d-4b3b-88e2-7d6f6c9cc8a7");
            var pdvUserId = Guid.Parse("a9d5e5aa-6da8-4adf-9b32-35a0b8a8f51a");
            var waiterUserId = Guid.Parse("6f6d64a5-1d6f-4c3a-8f90-7e245c3bd79a");
            var totemUserId = Guid.Parse("4b20cb61-70b2-4d8f-8fb1-53e0c2d2db5f");

            var hamburgersId = Guid.Parse("3b1a0c6e-f0a3-4c74-8c6b-e5dc0d8a9f77");
            var sidesId = Guid.Parse("c7b69d4e-1f1b-45f1-bb9a-72c4c8b44c1f");
            var drinksId = Guid.Parse("0fefaa50-4d5a-4c48-a238-9ed5311f96d2");
            var dessertsId = Guid.Parse("dd0c055d-0b78-4d8b-9ec7-3c4e5e4a4c0a");
            var suppliesId = Guid.Parse("1c3d2ea8-6a26-4f14-987f-0c9f4fd1b98c");

            var burgerId = Guid.Parse("b0701f66-512d-4c3d-9b1f-9877a6b8c5a1");
            var friesSmallId = Guid.Parse("c1c2c30b-7f0f-4ff0-9c42-589d07d3a4c7");
            var coke350Id = Guid.Parse("9b1a1f39-52a0-476f-a0fd-3d2d4fb0c070");
            var water500Id = Guid.Parse("a1ab3dfe-0d08-4a5c-9c1c-6c00a3b5a9e1");
            var sundaeId = Guid.Parse("3a3d4d92-f3b2-4b3c-a96c-c08a3f44b563");
            var comboId = Guid.Parse("39b1b3aa-cd5c-48a0-9cf7-9f0bdbb7e0b7");

            var potatoRawId = Guid.Parse("d2b2b4a8-6fd3-48cc-9ccf-97de0fb3b8aa");
            var beefRawId = Guid.Parse("1b8b2b15-9a54-4d45-9cb2-2a5bd5d59df6");
            var bunId = Guid.Parse("3d1a3e6c-9c9c-4d67-8c1d-63d58d4e8f0d");
            var cheeseId = Guid.Parse("5b6e7c0c-8b2f-4d35-a4b6-1c6c0d7b9c2e");

            var createdAt = new DateTimeOffset(2026, 04, 04, 0, 0, 0, TimeSpan.Zero);
            var createdAtIso = createdAt.ToString("O");
            var passwordHash = HashPasswordDeterministic("123456");

            migrationBuilder.Sql(
                $"""
                INSERT INTO tenants (Id, Name, NormalizedName, CreatedAt)
                VALUES ('{tenantId:D}', 'TANZ', 'TANZ', '{createdAtIso}');
                """
            );

            migrationBuilder.Sql(
                $"""
                INSERT INTO users (Id, TenantId, Email, NormalizedEmail, PasswordHash, Role, CreatedAt)
                VALUES
                  ('{adminUserId:D}', '{tenantId:D}', 'admin@tanz.local', 'admin@tanz.local', '{passwordHash}', 1, '{createdAtIso}'),
                  ('{staffUserId:D}', '{tenantId:D}', 'staff@tanz.local', 'staff@tanz.local', '{passwordHash}', 2, '{createdAtIso}'),
                  ('{kitchenUserId:D}', '{tenantId:D}', 'kitchen@tanz.local', 'kitchen@tanz.local', '{passwordHash}', 2, '{createdAtIso}'),
                  ('{pdvUserId:D}', '{tenantId:D}', 'pdv@tanz.local', 'pdv@tanz.local', '{passwordHash}', 5, '{createdAtIso}'),
                  ('{waiterUserId:D}', '{tenantId:D}', 'waiter@tanz.local', 'waiter@tanz.local', '{passwordHash}', 4, '{createdAtIso}'),
                  ('{totemUserId:D}', '{tenantId:D}', 'totem@tanz.local', 'totem@tanz.local', '{passwordHash}', 3, '{createdAtIso}');
                """
            );

            migrationBuilder.Sql(
                $"""
                INSERT INTO categories (Id, TenantId, Code, Slug, Name, IsActive, CreatedAt, UpdatedAt)
                VALUES
                  ('{hamburgersId:D}', '{tenantId:D}', '00001', 'hamburgers', 'Hambúrgueres', 1, '{createdAtIso}', '{createdAtIso}'),
                  ('{sidesId:D}', '{tenantId:D}', '00002', 'acompanhamentos', 'Acompanhamentos', 1, '{createdAtIso}', '{createdAtIso}'),
                  ('{drinksId:D}', '{tenantId:D}', '00003', 'bebidas', 'Bebidas', 1, '{createdAtIso}', '{createdAtIso}'),
                  ('{dessertsId:D}', '{tenantId:D}', '00004', 'sobremesas', 'Sobremesas', 1, '{createdAtIso}', '{createdAtIso}'),
                  ('{suppliesId:D}', '{tenantId:D}', '00005', 'insumos', 'Insumos', 1, '{createdAtIso}', '{createdAtIso}');
                """
            );

            migrationBuilder.Sql(
                $"""
                INSERT INTO skus (
                    Id, TenantId, CategoryCode, Code, NormalizedCode, Name, PriceCents, AveragePrepSeconds, ImageUrl,
                    NfeCProd, NfeCEan, NfeCfop, NfeUCom, NfeQCom, NfeVUnCom, NfeVProd, NfeCEanTrib, NfeUTrib, NfeQTrib, NfeVUnTrib,
                    NfeIcmsOrig, NfeIcmsCst, NfeIcmsModBc, NfeIcmsVBc, NfeIcmsPIcms, NfeIcmsVIcms,
                    NfePisCst, NfePisVBc, NfePisPPis, NfePisVPis,
                    NfeCofinsCst, NfeCofinsVBc, NfeCofinsPCofins, NfeCofinsVCofins,
                    TracksStock, StockBaseUnit, StockOnHandBaseQty, IsActive, CreatedAt, UpdatedAt
                )
                VALUES
                  ('{burgerId:D}', '{tenantId:D}', '00001', 'TZ-BURGER-CLASSIC', 'TZ-BURGER-CLASSIC', 'TZ Burger Classic', 2890, 480, NULL,
                    NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL,
                    NULL, NULL, NULL, NULL, NULL, NULL,
                    NULL, NULL, NULL, NULL,
                    NULL, NULL, NULL, NULL,
                    0, NULL, NULL, 1, '{createdAtIso}', '{createdAtIso}'),
                  ('{friesSmallId:D}', '{tenantId:D}', '00002', 'BATATA-P', 'BATATA-P', 'Batata Frita P', 1290, 300, NULL,
                    NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL,
                    NULL, NULL, NULL, NULL, NULL, NULL,
                    NULL, NULL, NULL, NULL,
                    NULL, NULL, NULL, NULL,
                    0, NULL, NULL, 1, '{createdAtIso}', '{createdAtIso}'),
                  ('{coke350Id:D}', '{tenantId:D}', '00003', 'COCA-350', 'COCA-350', 'Coca-Cola Lata 350ml', 890, NULL, NULL,
                    NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL,
                    NULL, NULL, NULL, NULL, NULL, NULL,
                    NULL, NULL, NULL, NULL,
                    NULL, NULL, NULL, NULL,
                    1, 0, 60, 1, '{createdAtIso}', '{createdAtIso}'),
                  ('{water500Id:D}', '{tenantId:D}', '00003', 'AGUA-500', 'AGUA-500', 'Água Mineral 500ml', 590, NULL, NULL,
                    NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL,
                    NULL, NULL, NULL, NULL, NULL, NULL,
                    NULL, NULL, NULL, NULL,
                    NULL, NULL, NULL, NULL,
                    1, 0, 40, 1, '{createdAtIso}', '{createdAtIso}'),
                  ('{sundaeId:D}', '{tenantId:D}', '00004', 'TZ-SUNDAE', 'TZ-SUNDAE', 'Sundae Chocolate', 1090, 120, NULL,
                    NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL,
                    NULL, NULL, NULL, NULL, NULL, NULL,
                    NULL, NULL, NULL, NULL,
                    NULL, NULL, NULL, NULL,
                    0, NULL, NULL, 1, '{createdAtIso}', '{createdAtIso}'),
                  ('{comboId:D}', '{tenantId:D}', '00001', 'TZ-COMBO-CLASSIC', 'TZ-COMBO-CLASSIC', 'Combo Classic (Burger + Batata P + Bebida)', 4490, 480, NULL,
                    NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL,
                    NULL, NULL, NULL, NULL, NULL, NULL,
                    NULL, NULL, NULL, NULL,
                    NULL, NULL, NULL, NULL,
                    0, NULL, NULL, 1, '{createdAtIso}', '{createdAtIso}');
                """
            );

            migrationBuilder.Sql(
                $"""
                INSERT INTO skus (
                    Id, TenantId, CategoryCode, Code, NormalizedCode, Name, PriceCents, AveragePrepSeconds, ImageUrl,
                    NfeCProd, NfeCEan, NfeCfop, NfeUCom, NfeQCom, NfeVUnCom, NfeVProd, NfeCEanTrib, NfeUTrib, NfeQTrib, NfeVUnTrib,
                    NfeIcmsOrig, NfeIcmsCst, NfeIcmsModBc, NfeIcmsVBc, NfeIcmsPIcms, NfeIcmsVIcms,
                    NfePisCst, NfePisVBc, NfePisPPis, NfePisVPis,
                    NfeCofinsCst, NfeCofinsVBc, NfeCofinsPCofins, NfeCofinsVCofins,
                    TracksStock, StockBaseUnit, StockOnHandBaseQty, IsActive, CreatedAt, UpdatedAt
                )
                VALUES
                  ('{potatoRawId:D}', '{tenantId:D}', '00005', 'INS-BATATA-CRUA', 'INS-BATATA-CRUA', 'Batata in natura (g)', 0, NULL, NULL,
                    NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL,
                    NULL, NULL, NULL, NULL, NULL, NULL,
                    NULL, NULL, NULL, NULL,
                    NULL, NULL, NULL, NULL,
                    1, 1, 25000, 1, '{createdAtIso}', '{createdAtIso}'),
                  ('{beefRawId:D}', '{tenantId:D}', '00005', 'INS-CARNE-BOI', 'INS-CARNE-BOI', 'Carne bovina moída (g)', 0, NULL, NULL,
                    NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL,
                    NULL, NULL, NULL, NULL, NULL, NULL,
                    NULL, NULL, NULL, NULL,
                    NULL, NULL, NULL, NULL,
                    1, 1, 12000, 1, '{createdAtIso}', '{createdAtIso}'),
                  ('{bunId:D}', '{tenantId:D}', '00005', 'INS-PAO-HAMB', 'INS-PAO-HAMB', 'Pão de hambúrguer (un)', 0, NULL, NULL,
                    NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL,
                    NULL, NULL, NULL, NULL, NULL, NULL,
                    NULL, NULL, NULL, NULL,
                    NULL, NULL, NULL, NULL,
                    1, 0, 100, 1, '{createdAtIso}', '{createdAtIso}'),
                  ('{cheeseId:D}', '{tenantId:D}', '00005', 'INS-QUEIJO', 'INS-QUEIJO', 'Queijo fatiado (un)', 0, NULL, NULL,
                    NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL,
                    NULL, NULL, NULL, NULL, NULL, NULL,
                    NULL, NULL, NULL, NULL,
                    NULL, NULL, NULL, NULL,
                    1, 0, 100, 1, '{createdAtIso}', '{createdAtIso}');
                """
            );

            migrationBuilder.Sql(
                $"""
                INSERT INTO sku_stock_consumptions (Id, TenantId, SkuId, SourceSkuId, QuantityBase)
                VALUES
                  ('1b9fe6ef-3f15-4f4c-9aa3-8511f8b0c2c3', '{tenantId:D}', '{friesSmallId:D}', '{potatoRawId:D}', 150),
                  ('8fb6c5d0-4f9c-4c7d-98b0-70e4f3e7b0e1', '{tenantId:D}', '{burgerId:D}', '{bunId:D}', 1),
                  ('d3d2d9a1-8d5e-4a4b-8f2c-4b5c6d7e8f01', '{tenantId:D}', '{burgerId:D}', '{beefRawId:D}', 160),
                  ('8b2f7c21-59ce-4d38-9c2f-9fe1c1c85ac7', '{tenantId:D}', '{burgerId:D}', '{cheeseId:D}', 1);
                """
            );

            migrationBuilder.Sql(
                $"""
                INSERT INTO kitchen_sla (TenantId, QueuedTargetSeconds, PreparationBaseTargetSeconds, ReadyTargetSeconds, UpdatedAt)
                VALUES ('{tenantId:D}', 120, 480, 120, '{createdAtIso}');
                """
            );

            migrationBuilder.Sql(
                $"""
                INSERT INTO sku_stock_ledger (Id, TenantId, SkuId, DeltaBaseQty, StockAfterBaseQty, OriginType, OriginId, Notes, ActorUserId, CreatedAt)
                VALUES
                  ('c2c0f5fb-9e2a-4dcb-90b0-3f21a2b0f9e1', '{tenantId:D}', '{coke350Id:D}', 60, 60, 0, NULL, 'seed', '{adminUserId:D}', '{createdAtIso}'),
                  ('5f0a6c1a-3f5a-4c0d-9b1c-4e9f1a2b3c4d', '{tenantId:D}', '{water500Id:D}', 40, 40, 0, NULL, 'seed', '{adminUserId:D}', '{createdAtIso}'),
                  ('6c9b0c2d-1e3f-4a5b-9c7d-2e1f0a9b8c7d', '{tenantId:D}', '{potatoRawId:D}', 25000, 25000, 0, NULL, 'seed', '{adminUserId:D}', '{createdAtIso}'),
                  ('b2d9f0a1-2c3d-4e5f-8a9b-0c1d2e3f4a5b', '{tenantId:D}', '{beefRawId:D}', 12000, 12000, 0, NULL, 'seed', '{adminUserId:D}', '{createdAtIso}'),
                  ('9a8b7c6d-5e4f-3a2b-1c0d-9e8f7a6b5c4d', '{tenantId:D}', '{bunId:D}', 100, 100, 0, NULL, 'seed', '{adminUserId:D}', '{createdAtIso}'),
                  ('0a1b2c3d-4e5f-6071-8293-a4b5c6d7e8f9', '{tenantId:D}', '{cheeseId:D}', 100, 100, 0, NULL, 'seed', '{adminUserId:D}', '{createdAtIso}');
                """
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var tenantId = Guid.Parse("7c0b9c35-2fcb-4af0-82cc-6868ea6f3bf0");

            migrationBuilder.Sql(
                $"""
                DELETE FROM sku_stock_ledger WHERE TenantId = '{tenantId:D}' AND Notes = 'seed';
                DELETE FROM sku_stock_consumptions WHERE TenantId = '{tenantId:D}';
                DELETE FROM kitchen_sla WHERE TenantId = '{tenantId:D}';
                DELETE FROM skus WHERE TenantId = '{tenantId:D}';
                DELETE FROM categories WHERE TenantId = '{tenantId:D}' AND Slug IN ('hamburgers','acompanhamentos','bebidas','sobremesas','insumos');
                DELETE FROM users WHERE TenantId = '{tenantId:D}';
                DELETE FROM tenants WHERE Id = '{tenantId:D}';
                """
            );
        }

        private static string HashPasswordDeterministic(string password)
        {
            var salt = new byte[]
            {
                1, 2, 3, 4, 5, 6, 7, 8,
                9, 10, 11, 12, 13, 14, 15, 16
            };

            const int iterations = 100_000;
            const int keySize = 32;

            var hash = Rfc2898DeriveBytes.Pbkdf2(
                password: password,
                salt: salt,
                iterations: iterations,
                hashAlgorithm: HashAlgorithmName.SHA256,
                outputLength: keySize
            );

            return $"v1.{iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
        }
    }
}
