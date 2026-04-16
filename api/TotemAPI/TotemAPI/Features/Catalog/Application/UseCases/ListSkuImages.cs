using TotemAPI.Features.Catalog.Application.Abstractions;

namespace TotemAPI.Features.Catalog.Application.UseCases;

public sealed record ListSkuImagesQuery(Guid TenantId, Guid SkuId);

public sealed record SkuImageResult(Guid Id, string Url, DateTimeOffset CreatedAt);

public sealed class ListSkuImages
{
    private readonly ISkuRepository _skus;

    public ListSkuImages(ISkuRepository skus)
    {
        _skus = skus;
    }

    public async Task<IReadOnlyList<SkuImageResult>> HandleAsync(ListSkuImagesQuery query, CancellationToken ct)
    {
        if (query.TenantId == Guid.Empty) throw new ArgumentException("TenantId inválido.");
        if (query.SkuId == Guid.Empty) throw new ArgumentException("SkuId inválido.");

        var sku = await _skus.GetByIdAsync(query.TenantId, query.SkuId, ct);
        if (sku is null) return Array.Empty<SkuImageResult>();

        var images = await _skus.ListImagesAsync(query.TenantId, query.SkuId, ct);
        return images.Select(x => new SkuImageResult(x.Id, x.Url, x.CreatedAt)).ToList().AsReadOnly();
    }
}

