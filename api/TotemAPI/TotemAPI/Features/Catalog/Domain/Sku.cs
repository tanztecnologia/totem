namespace TotemAPI.Features.Catalog.Domain;

public enum StockBaseUnit
{
    Unit = 0,
    Gram = 1,
    Milliliter = 2
}

public enum StockLedgerOriginType
{
    InitialStock = 0,
    ManualEntry = 1,
    OrderPayment = 2,
    ManualAdjustment = 3,
}

public sealed record SkuStockConsumption(
    Guid Id,
    Guid TenantId,
    Guid SkuId,
    Guid SourceSkuId,
    decimal QuantityBase
);

public sealed record SkuStockLedgerEntry(
    Guid Id,
    Guid TenantId,
    Guid SkuId,
    decimal DeltaBaseQty,
    decimal StockAfterBaseQty,
    StockLedgerOriginType OriginType,
    Guid? OriginId,
    string? Notes,
    Guid? ActorUserId,
    DateTimeOffset CreatedAt
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
    bool TracksStock,
    StockBaseUnit? StockBaseUnit,
    decimal? StockOnHandBaseQty,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);
