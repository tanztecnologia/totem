using TotemAPI.Features.Catalog.Application.Abstractions;
using TotemAPI.Features.Catalog.Domain;

namespace TotemAPI.Features.Catalog.Application.UseCases;

public sealed record CreateSkuCommand(
    Guid TenantId,
    string Code,
    string Name,
    int PriceCents,
    string? ImageUrl,
    bool IsActive
);

public sealed record SkuResult(
    Guid Id,
    Guid TenantId,
    string Code,
    string Name,
    int PriceCents,
    string? ImageUrl,
    bool IsActive
);

public sealed class CreateSku
{
    public CreateSku(ISkuRepository skus)
    {
        _skus = skus;
    }

    private readonly ISkuRepository _skus;

    public async Task<SkuResult> HandleAsync(CreateSkuCommand command, CancellationToken ct)
    {
        if (command.TenantId == Guid.Empty) throw new ArgumentException("TenantId inválido.");

        var code = (command.Code ?? string.Empty).Trim();
        var name = (command.Name ?? string.Empty).Trim();
        var imageUrl = string.IsNullOrWhiteSpace(command.ImageUrl) ? null : command.ImageUrl.Trim();

        if (code.Length < 2) throw new ArgumentException("Code inválido.");
        if (name.Length < 2) throw new ArgumentException("Name inválido.");
        if (command.PriceCents < 0) throw new ArgumentException("PriceCents inválido.");

        var existing = await _skus.GetByCodeAsync(command.TenantId, code, ct);
        if (existing is not null) throw new InvalidOperationException("SKU já existe.");

        var now = DateTimeOffset.UtcNow;
        var sku = new Sku(
            Id: Guid.NewGuid(),
            TenantId: command.TenantId,
            Code: code,
            Name: name,
            PriceCents: command.PriceCents,
            ImageUrl: imageUrl,
            IsActive: command.IsActive,
            CreatedAt: now,
            UpdatedAt: now
        );

        await _skus.AddAsync(sku, ct);
        return new SkuResult(sku.Id, sku.TenantId, sku.Code, sku.Name, sku.PriceCents, sku.ImageUrl, sku.IsActive);
    }
}
