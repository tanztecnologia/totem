using Microsoft.EntityFrameworkCore;

namespace TotemAPI.Infrastructure.Persistence;

public static class SchemaBootstrapper
{
    public static async Task EnsureSkuImagesTableAsync(TotemDbContext db, CancellationToken ct)
    {
        var provider = db.Database.ProviderName ?? string.Empty;
        var isMySql = provider.Contains("MySql", StringComparison.OrdinalIgnoreCase);

        var sql = isMySql
            ? """
              CREATE TABLE IF NOT EXISTS sku_images (
                  Id varchar(36) NOT NULL,
                  TenantId varchar(36) NOT NULL,
                  SkuId varchar(36) NOT NULL,
                  S3Key varchar(1024) NOT NULL,
                  Url varchar(2048) NOT NULL,
                  CreatedAt datetime(6) NOT NULL,
                  PRIMARY KEY (Id),
                  KEY IX_sku_images_tenant_sku (TenantId, SkuId),
                  KEY IX_sku_images_tenant_sku_created (TenantId, SkuId, CreatedAt)
              );
              """
            : """
              CREATE TABLE IF NOT EXISTS sku_images (
                  Id TEXT NOT NULL PRIMARY KEY,
                  TenantId TEXT NOT NULL,
                  SkuId TEXT NOT NULL,
                  S3Key TEXT NOT NULL,
                  Url TEXT NOT NULL,
                  CreatedAt TEXT NOT NULL
              );
              CREATE INDEX IF NOT EXISTS IX_sku_images_tenant_sku ON sku_images (TenantId, SkuId);
              CREATE INDEX IF NOT EXISTS IX_sku_images_tenant_sku_created ON sku_images (TenantId, SkuId, CreatedAt);
              """;

        await db.Database.ExecuteSqlRawAsync(sql, ct);
    }
}

