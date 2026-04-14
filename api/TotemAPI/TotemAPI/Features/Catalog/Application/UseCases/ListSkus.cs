using TotemAPI.Features.Catalog.Application.Abstractions;

namespace TotemAPI.Features.Catalog.Application.UseCases;

public sealed record ListSkusQuery(Guid TenantId);

public sealed class ListSkus
{
    public ListSkus(ISkuRepository skus)
    {
        _skus = skus;
    }

    private readonly ISkuRepository _skus;

    public async Task<IReadOnlyList<SkuResult>> HandleAsync(ListSkusQuery query, CancellationToken ct)
    {
        if (query.TenantId == Guid.Empty) throw new ArgumentException("TenantId inválido.");

        var list = await _skus.ListAsync(query.TenantId, ct);
        return list
            .Select(s => new SkuResult(s.Id, s.TenantId, s.CategoryCode, s.Code, s.Name, s.PriceCents, s.AveragePrepSeconds, s.ImageUrl, s.IsActive))
            .ToList();
    }
}
