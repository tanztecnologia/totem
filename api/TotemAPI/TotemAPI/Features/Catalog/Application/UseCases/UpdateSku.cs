using TotemAPI.Features.Catalog.Application.Abstractions;
using TotemAPI.Features.Catalog.Domain;

namespace TotemAPI.Features.Catalog.Application.UseCases;

public sealed record UpdateSkuCommand(
    Guid TenantId,
    Guid SkuId,
    string Code,
    string Name,
    int PriceCents,
    int? AveragePrepSeconds,
    string? ImageUrl,
    bool IsActive
);

public sealed class UpdateSku
{
    public UpdateSku(ISkuRepository skus)
    {
        _skus = skus;
    }

    private readonly ISkuRepository _skus;

    public async Task<SkuResult?> HandleAsync(UpdateSkuCommand command, CancellationToken ct)
    {
        if (command.TenantId == Guid.Empty) throw new ArgumentException("TenantId inválido.");
        if (command.SkuId == Guid.Empty) throw new ArgumentException("SkuId inválido.");

        var code = (command.Code ?? string.Empty).Trim();
        var name = (command.Name ?? string.Empty).Trim();
        var imageUrl = string.IsNullOrWhiteSpace(command.ImageUrl) ? null : command.ImageUrl.Trim();

        if (code.Length < 2) throw new ArgumentException("Code inválido.");
        if (name.Length < 2) throw new ArgumentException("Name inválido.");
        if (command.PriceCents < 0) throw new ArgumentException("PriceCents inválido.");
        if (command.AveragePrepSeconds is not null && command.AveragePrepSeconds <= 0)
            throw new ArgumentException("AveragePrepSeconds inválido.");

        var current = await _skus.GetByIdAsync(command.TenantId, command.SkuId, ct);
        if (current is null) return null;

        var existingCode = await _skus.GetByCodeAsync(command.TenantId, code, ct);
        if (existingCode is not null && existingCode.Id != command.SkuId) throw new InvalidOperationException("Code já está em uso.");

        var updated = new Sku(
            Id: current.Id,
            TenantId: current.TenantId,
            Code: code,
            Name: name,
            PriceCents: command.PriceCents,
            AveragePrepSeconds: command.AveragePrepSeconds,
            ImageUrl: imageUrl,
            IsActive: command.IsActive,
            CreatedAt: current.CreatedAt,
            UpdatedAt: DateTimeOffset.UtcNow
        );

        await _skus.UpdateAsync(updated, ct);
        return new SkuResult(updated.Id, updated.TenantId, updated.Code, updated.Name, updated.PriceCents, updated.AveragePrepSeconds, updated.ImageUrl, updated.IsActive);
    }
}
