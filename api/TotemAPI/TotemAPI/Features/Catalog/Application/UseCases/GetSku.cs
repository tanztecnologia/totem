using TotemAPI.Features.Catalog.Application.Abstractions;

namespace TotemAPI.Features.Catalog.Application.UseCases;

public sealed record GetSkuQuery(Guid TenantId, Guid SkuId);

public sealed class GetSku
{
    public GetSku(ISkuRepository skus)
    {
        _skus = skus;
    }

    private readonly ISkuRepository _skus;

    public async Task<SkuResult?> HandleAsync(GetSkuQuery query, CancellationToken ct)
    {
        if (query.TenantId == Guid.Empty) throw new ArgumentException("TenantId inválido.");
        if (query.SkuId == Guid.Empty) throw new ArgumentException("SkuId inválido.");

        var sku = await _skus.GetByIdAsync(query.TenantId, query.SkuId, ct);
        if (sku is null) return null;
        return new SkuResult(sku.Id, sku.TenantId, sku.CategoryCode, sku.Code, sku.Name, sku.PriceCents, sku.AveragePrepSeconds, sku.ImageUrl, sku.IsActive);
    }
}
