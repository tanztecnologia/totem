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
        var sourceIds = list.Select(x => x.SourceSkuId).Distinct().ToList();
        var codeById = new Dictionary<Guid, string>();
        foreach (var id in sourceIds)
        {
            var source = await _skus.GetByIdAsync(query.TenantId, id, ct);
            if (source is null) continue;
            codeById[id] = source.Code;
        }

        return list
            .Select(
                x =>
                    new SkuStockConsumptionResult(
                        x.Id,
                        x.SkuId,
                        x.SourceSkuId,
                        codeById.TryGetValue(x.SourceSkuId, out var c) ? c : string.Empty,
                        x.QuantityBase
                    )
            )
            .ToList()
            .AsReadOnly();
    }
}
