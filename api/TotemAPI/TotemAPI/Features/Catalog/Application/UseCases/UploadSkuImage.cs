using TotemAPI.Features.Catalog.Application.Abstractions;
using TotemAPI.Features.Catalog.Domain;
using TotemAPI.Infrastructure.Storage;

namespace TotemAPI.Features.Catalog.Application.UseCases;

public sealed record UploadSkuImageCommand(
    Guid TenantId,
    Guid SkuId,
    string FileName,
    string ContentType,
    Stream Content
);

public sealed class UploadSkuImage
{
    private readonly ISkuRepository _skus;
    private readonly ISkuImageStorage _storage;

    public UploadSkuImage(ISkuRepository skus, ISkuImageStorage storage)
    {
        _skus = skus;
        _storage = storage;
    }

    public async Task<SkuImageResult> HandleAsync(UploadSkuImageCommand command, CancellationToken ct)
    {
        if (command.TenantId == Guid.Empty) throw new ArgumentException("TenantId inválido.");
        if (command.SkuId == Guid.Empty) throw new ArgumentException("SkuId inválido.");
        if (command.Content is null) throw new ArgumentException("Arquivo inválido.");

        var sku = await _skus.GetByIdAsync(command.TenantId, command.SkuId, ct);
        if (sku is null) throw new InvalidOperationException("SKU não encontrado.");

        var count = await _skus.CountImagesAsync(command.TenantId, command.SkuId, ct);
        if (count >= 3) throw new InvalidOperationException("O SKU já possui o limite máximo de 3 fotos.");

        var now = DateTimeOffset.UtcNow;
        var uploaded = await _storage.UploadAsync(
            command.TenantId,
            command.SkuId,
            command.FileName,
            command.ContentType,
            command.Content,
            ct
        );

        var image = new SkuImage(
            Id: Guid.NewGuid(),
            TenantId: command.TenantId,
            SkuId: command.SkuId,
            S3Key: uploaded.Key,
            Url: uploaded.Url,
            CreatedAt: now
        );

        await _skus.AddImageAsync(image, ct);
        return new SkuImageResult(image.Id, image.Url, image.CreatedAt);
    }
}

