using Microsoft.EntityFrameworkCore;
using TotemAPI.Features.Catalog.Application.Abstractions;
using TotemAPI.Features.Catalog.Domain;
using TotemAPI.Infrastructure.Persistence;

namespace TotemAPI.Features.Catalog.Application.UseCases;

public sealed record CreateCategoryCommand(Guid TenantId, string Name, string? Slug, bool IsActive);

public sealed record CategoryResult(Guid Id, Guid TenantId, string Code, string Slug, string Name, bool IsActive);

public sealed class CreateCategory
{
    public CreateCategory(ICategoryRepository categories)
    {
        _categories = categories;
    }

    private readonly ICategoryRepository _categories;

    public async Task<CategoryResult> HandleAsync(CreateCategoryCommand command, CancellationToken ct)
    {
        if (command.TenantId == Guid.Empty) throw new ArgumentException("TenantId inválido.");

        var name = (command.Name ?? string.Empty).Trim();
        var slug = CategoryMapping.NormalizeSlug(string.IsNullOrWhiteSpace(command.Slug) ? name : command.Slug!);

        if (name.Length < 2) throw new ArgumentException("Name inválido.");
        if (slug.Length < 2) throw new ArgumentException("Slug inválido.");

        var existingSlug = await _categories.GetBySlugAsync(command.TenantId, slug, ct);
        if (existingSlug is not null) throw new InvalidOperationException("Categoria já existe.");

        var now = DateTimeOffset.UtcNow;
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var max = await _categories.GetMaxCodeNumberAsync(command.TenantId, ct);
            var nextCode = (max + 1).ToString("D5");

            var category = new Category(
                Id: Guid.NewGuid(),
                TenantId: command.TenantId,
                Code: nextCode,
                Slug: slug,
                Name: name,
                IsActive: command.IsActive,
                CreatedAt: now,
                UpdatedAt: now
            );

            try
            {
                await _categories.AddAsync(category, ct);
                return new CategoryResult(category.Id, category.TenantId, category.Code, category.Slug, category.Name, category.IsActive);
            }
            catch (DbUpdateException) when (attempt < 2)
            {
            }
        }

        throw new InvalidOperationException("Falha ao gerar código da categoria.");
    }
}
