using Microsoft.EntityFrameworkCore;
using TotemAPI.Features.Catalog.Application.Abstractions;
using TotemAPI.Features.Catalog.Domain;

namespace TotemAPI.Features.Catalog.Application.UseCases;

public sealed record CreateSkuCommand(
    Guid TenantId,
    string CategoryCode,
    string Name,
    int PriceCents,
    int? AveragePrepSeconds,
    string? ImageUrl,
    bool IsActive
);

public sealed record SkuResult(
    Guid Id,
    Guid TenantId,
    string CategoryCode,
    string Code,
    string Name,
    int PriceCents,
    int? AveragePrepSeconds,
    string? ImageUrl,
    bool IsActive
);

public sealed class CreateSku
{
    public CreateSku(ISkuRepository skus, ICategoryRepository categories)
    {
        _skus = skus;
        _categories = categories;
    }

    private readonly ISkuRepository _skus;
    private readonly ICategoryRepository _categories;

    public async Task<SkuResult> HandleAsync(CreateSkuCommand command, CancellationToken ct)
    {
        if (command.TenantId == Guid.Empty) throw new ArgumentException("TenantId inválido.");
        var categoryCode = (command.CategoryCode ?? string.Empty).Trim();
        if (categoryCode.Length < 1) throw new ArgumentException("CategoryCode inválido.");

        var name = (command.Name ?? string.Empty).Trim();
        var imageUrl = string.IsNullOrWhiteSpace(command.ImageUrl) ? null : command.ImageUrl.Trim();

        if (name.Length < 2) throw new ArgumentException("Name inválido.");
        if (command.PriceCents < 0) throw new ArgumentException("PriceCents inválido.");
        if (command.AveragePrepSeconds is not null && command.AveragePrepSeconds <= 0)
            throw new ArgumentException("AveragePrepSeconds inválido.");

        var category = await _categories.GetByCodeAsync(command.TenantId, categoryCode, ct);
        if (category is null) throw new InvalidOperationException("Categoria não encontrada.");

        var now = DateTimeOffset.UtcNow;
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var max = await _skus.GetMaxCodeNumberAsync(command.TenantId, ct);
            var nextCode = (max + 1).ToString("D5");

            var sku = new Sku(
                Id: Guid.NewGuid(),
                TenantId: command.TenantId,
                CategoryCode: category.Code,
                Code: nextCode,
                Name: name,
                PriceCents: command.PriceCents,
                AveragePrepSeconds: command.AveragePrepSeconds,
                ImageUrl: imageUrl,
                NfeCProd: null,
                NfeCEan: null,
                NfeCfop: null,
                NfeUCom: null,
                NfeQCom: null,
                NfeVUnCom: null,
                NfeVProd: null,
                NfeCEanTrib: null,
                NfeUTrib: null,
                NfeQTrib: null,
                NfeVUnTrib: null,
                IsActive: command.IsActive,
                CreatedAt: now,
                UpdatedAt: now
            );

            try
            {
                await _skus.AddAsync(sku, ct);
                return new SkuResult(sku.Id, sku.TenantId, sku.CategoryCode, sku.Code, sku.Name, sku.PriceCents, sku.AveragePrepSeconds, sku.ImageUrl, sku.IsActive);
            }
            catch (DbUpdateException) when (attempt < 2)
            {
            }
        }

        throw new InvalidOperationException("Falha ao gerar código do SKU.");
    }
}
