using TotemAPI.Features.Catalog.Application.Abstractions;

namespace TotemAPI.Features.Catalog.Application.UseCases;

public sealed record DeleteSkuCommand(Guid TenantId, Guid SkuId);

public sealed class DeleteSku
{
    public DeleteSku(ISkuRepository skus)
    {
        _skus = skus;
    }

    private readonly ISkuRepository _skus;

    public async Task<bool> HandleAsync(DeleteSkuCommand command, CancellationToken ct)
    {
        if (command.TenantId == Guid.Empty) throw new ArgumentException("TenantId inválido.");
        if (command.SkuId == Guid.Empty) throw new ArgumentException("SkuId inválido.");

        var existing = await _skus.GetByIdAsync(command.TenantId, command.SkuId, ct);
        if (existing is null) return false;

        await _skus.DeleteAsync(command.TenantId, command.SkuId, ct);
        return true;
    }
}

