using TotemAPI.Features.Catalog.Application.Abstractions;
using TotemAPI.Infrastructure.Storage;

namespace TotemAPI.Features.Catalog.Application.UseCases;

public sealed record ListSkuImagesQuery(Guid TenantId, Guid SkuId);

public sealed record SkuImageResult(Guid Id, string Url, DateTimeOffset CreatedAt);

public sealed class ListSkuImages
{
    private readonly ISkuRepository _skus;
    private readonly ISkuImageStorage _storage;

    public ListSkuImages(ISkuRepository skus, ISkuImageStorage storage)
    {
        _skus = skus;
        _storage = storage;
    }

    public async Task<IReadOnlyList<SkuImageResult>> HandleAsync(ListSkuImagesQuery query, CancellationToken ct)
    {
        if (query.TenantId == Guid.Empty) throw new ArgumentException("TenantId inválido.");
        if (query.SkuId == Guid.Empty) throw new ArgumentException("SkuId inválido.");

        var sku = await _skus.GetByIdAsync(query.TenantId, query.SkuId, ct);
        if (sku is null) return Array.Empty<SkuImageResult>();

        var images = await _skus.ListImagesAsync(query.TenantId, query.SkuId, ct);
        var cleaned = new List<SkuImageResult>();
        foreach (var img in images)
        {
            if (await _storage.ExistsAsync(img.S3Key, ct))
            {
                cleaned.Add(new SkuImageResult(img.Id, img.Url, img.CreatedAt));
                continue;
            }

            await _skus.DeleteImageAsync(query.TenantId, query.SkuId, img.Id, ct);
        }

        return cleaned.AsReadOnly();
    }
}
