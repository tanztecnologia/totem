using TotemAPI.Features.Catalog.Application.Abstractions;
using TotemAPI.Features.Catalog.Domain;

namespace TotemAPI.Features.Catalog.Application.UseCases;

public sealed record ListSkuStockLedgerQuery(
    Guid TenantId,
    Guid SkuId,
    int Limit = 50,
    DateTimeOffset? CursorCreatedAt = null,
    Guid? CursorId = null
);

public sealed record SkuStockLedgerEntryResult(
    Guid Id,
    Guid SkuId,
    decimal DeltaBaseQty,
    decimal StockAfterBaseQty,
    StockLedgerOriginType OriginType,
    Guid? OriginId,
    string? Notes,
    Guid? ActorUserId,
    DateTimeOffset CreatedAt
);

public sealed class ListSkuStockLedger
{
    public ListSkuStockLedger(ISkuRepository skus)
    {
        _skus = skus;
    }

    private readonly ISkuRepository _skus;

    public async Task<IReadOnlyList<SkuStockLedgerEntryResult>?> HandleAsync(
        ListSkuStockLedgerQuery query,
        CancellationToken ct
    )
    {
        if (query.TenantId == Guid.Empty) throw new ArgumentException("TenantId inválido.");
        if (query.SkuId == Guid.Empty) throw new ArgumentException("SkuId inválido.");

        var sku = await _skus.GetByIdAsync(query.TenantId, query.SkuId, ct);
        if (sku is null) return null;

        var limit = Math.Clamp(query.Limit, 1, 200);

        var entries = await _skus.ListStockLedgerAsync(
            query.TenantId,
            query.SkuId,
            limit,
            query.CursorCreatedAt,
            query.CursorId,
            ct
        );

        return entries
            .Select(e => new SkuStockLedgerEntryResult(
                Id: e.Id,
                SkuId: e.SkuId,
                DeltaBaseQty: e.DeltaBaseQty,
                StockAfterBaseQty: e.StockAfterBaseQty,
                OriginType: e.OriginType,
                OriginId: e.OriginId,
                Notes: e.Notes,
                ActorUserId: e.ActorUserId,
                CreatedAt: e.CreatedAt
            ))
            .ToList()
            .AsReadOnly();
    }
}
