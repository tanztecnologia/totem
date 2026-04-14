using TotemAPI.Features.Catalog.Application.Abstractions;
using TotemAPI.Features.Catalog.Domain;

namespace TotemAPI.Features.Catalog.Application.UseCases;

public sealed record UpdateSkuCommand(
    Guid TenantId,
    Guid SkuId,
    string CategoryCode,
    string Name,
    int PriceCents,
    int? AveragePrepSeconds,
    string? ImageUrl,
    bool IsActive
);

public sealed class UpdateSku
{
    public UpdateSku(ISkuRepository skus, ICategoryRepository categories)
    {
        _skus = skus;
        _categories = categories;
    }

    private readonly ISkuRepository _skus;
    private readonly ICategoryRepository _categories;

    public async Task<SkuResult?> HandleAsync(UpdateSkuCommand command, CancellationToken ct)
    {
        if (command.TenantId == Guid.Empty) throw new ArgumentException("TenantId inválido.");
        if (command.SkuId == Guid.Empty) throw new ArgumentException("SkuId inválido.");
        var categoryCode = (command.CategoryCode ?? string.Empty).Trim();
        if (categoryCode.Length < 1) throw new ArgumentException("CategoryCode inválido.");

        var name = (command.Name ?? string.Empty).Trim();
        var imageUrl = string.IsNullOrWhiteSpace(command.ImageUrl) ? null : command.ImageUrl.Trim();

        if (name.Length < 2) throw new ArgumentException("Name inválido.");
        if (command.PriceCents < 0) throw new ArgumentException("PriceCents inválido.");
        if (command.AveragePrepSeconds is not null && command.AveragePrepSeconds <= 0)
            throw new ArgumentException("AveragePrepSeconds inválido.");

        var current = await _skus.GetByIdAsync(command.TenantId, command.SkuId, ct);
        if (current is null) return null;

        var category = await _categories.GetByCodeAsync(command.TenantId, categoryCode, ct);
        if (category is null) throw new InvalidOperationException("Categoria não encontrada.");

        var updated = new Sku(
            Id: current.Id,
            TenantId: current.TenantId,
            CategoryCode: category.Code,
            Code: current.Code,
            Name: name,
            PriceCents: command.PriceCents,
            AveragePrepSeconds: command.AveragePrepSeconds,
            ImageUrl: imageUrl,
            IsActive: command.IsActive,
            CreatedAt: current.CreatedAt,
            UpdatedAt: DateTimeOffset.UtcNow
        );

        await _skus.UpdateAsync(updated, ct);
        return new SkuResult(updated.Id, updated.TenantId, updated.CategoryCode, updated.Code, updated.Name, updated.PriceCents, updated.AveragePrepSeconds, updated.ImageUrl, updated.IsActive);
    }
}
