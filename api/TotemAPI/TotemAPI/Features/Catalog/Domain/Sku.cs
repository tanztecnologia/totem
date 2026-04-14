namespace TotemAPI.Features.Catalog.Domain;

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
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);
