namespace TotemAPI.Features.Catalog.Domain;

public enum StockBaseUnit
{
    Unit = 0,
    Gram = 1,
    Milliliter = 2
}

public sealed record SkuStockConsumption(
    Guid Id,
    Guid TenantId,
    Guid SkuId,
    Guid SourceSkuId,
    decimal QuantityBase
);

public sealed record Sku(
    Guid Id,
    Guid TenantId,
    string CategoryCode,
    string Code,
    string Name,
    int PriceCents,
    int? AveragePrepSeconds,
    string? ImageUrl,
    string? NfeCProd,
    string? NfeCEan,
    string? NfeCfop,
    string? NfeUCom,
    decimal? NfeQCom,
    decimal? NfeVUnCom,
    decimal? NfeVProd,
    string? NfeCEanTrib,
    string? NfeUTrib,
    decimal? NfeQTrib,
    decimal? NfeVUnTrib,
    string? NfeIcmsOrig,
    string? NfeIcmsCst,
    string? NfeIcmsModBc,
    decimal? NfeIcmsVBc,
    decimal? NfeIcmsPIcms,
    decimal? NfeIcmsVIcms,
    string? NfePisCst,
    decimal? NfePisVBc,
    decimal? NfePisPPis,
    decimal? NfePisVPis,
    string? NfeCofinsCst,
    decimal? NfeCofinsVBc,
    decimal? NfeCofinsPCofins,
    decimal? NfeCofinsVCofins,
    StockBaseUnit? StockBaseUnit,
    decimal? StockOnHandBaseQty,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);
