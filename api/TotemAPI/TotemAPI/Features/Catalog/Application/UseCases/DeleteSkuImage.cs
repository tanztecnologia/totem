using TotemAPI.Features.Catalog.Application.Abstractions;
using TotemAPI.Infrastructure.Storage;

namespace TotemAPI.Features.Catalog.Application.UseCases;

public sealed record DeleteSkuImageCommand(Guid TenantId, Guid SkuId, Guid ImageId);

public sealed class DeleteSkuImage
{
    private readonly ISkuRepository _skus;
    private readonly ISkuImageStorage _storage;

    public DeleteSkuImage(ISkuRepository skus, ISkuImageStorage storage)
    {
        _skus = skus;
        _storage = storage;
    }

    public async Task<bool> HandleAsync(DeleteSkuImageCommand command, CancellationToken ct)
    {
        if (command.TenantId == Guid.Empty) throw new ArgumentException("TenantId inválido.");
        if (command.SkuId == Guid.Empty) throw new ArgumentException("SkuId inválido.");
        if (command.ImageId == Guid.Empty) throw new ArgumentException("ImageId inválido.");

        var removed = await _skus.DeleteImageAsync(command.TenantId, command.SkuId, command.ImageId, ct);
        if (removed is null) return false;

        await _storage.DeleteAsync(removed.S3Key, ct);
        return true;
    }
}

