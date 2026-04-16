using TotemAPI.Features.Catalog.Application.Abstractions;

namespace TotemAPI.Features.Catalog.Application.UseCases;

public sealed record ListSkuStockConsumptionsQuery(
    Guid TenantId,
    Guid SkuId
);

public sealed class ListSkuStockConsumptions
{
    public ListSkuStockConsumptions(ISkuRepository skus)
    {
        _skus = skus;
    }

    private readonly ISkuRepository _skus;

    public async Task<IReadOnlyList<SkuStockConsumptionResult>> HandleAsync(ListSkuStockConsumptionsQuery query, CancellationToken ct)
    {
        if (query.TenantId == Guid.Empty) throw new ArgumentException("TenantId inválido.");
        if (query.SkuId == Guid.Empty) throw new ArgumentException("SkuId inválido.");

        var list = await _skus.ListStockConsumptionsAsync(query.TenantId, query.SkuId, ct);
        return list
            .Select(x => new SkuStockConsumptionResult(x.Id, x.SkuId, x.SourceSkuId, string.Empty, x.QuantityBase))
            .ToList()
            .AsReadOnly();
    }
}

