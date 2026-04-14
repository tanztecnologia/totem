using TotemAPI.Features.Catalog.Application.Abstractions;
using TotemAPI.Infrastructure.Persistence;

namespace TotemAPI.Features.Catalog.Application.UseCases;

public sealed record UpdateCategoryCommand(Guid TenantId, string Code, string Name, string? Slug, bool IsActive);

public sealed class UpdateCategory
{
    public UpdateCategory(ICategoryRepository categories)
    {
        _categories = categories;
    }

    private readonly ICategoryRepository _categories;

    public async Task<CategoryResult?> HandleAsync(UpdateCategoryCommand command, CancellationToken ct)
    {
        if (command.TenantId == Guid.Empty) throw new ArgumentException("TenantId inválido.");

        var code = CategoryMapping.NormalizeNumericCode(command.Code);
        var name = (command.Name ?? string.Empty).Trim();
        var slug = CategoryMapping.NormalizeSlug(string.IsNullOrWhiteSpace(command.Slug) ? name : command.Slug!);

        if (code.Length < 2) throw new ArgumentException("Code inválido.");
        if (name.Length < 2) throw new ArgumentException("Name inválido.");
        if (slug.Length < 2) throw new ArgumentException("Slug inválido.");

        var existing = await _categories.GetByCodeAsync(command.TenantId, code, ct);
        if (existing is null) return null;

        var other = await _categories.GetBySlugAsync(command.TenantId, slug, ct);
        if (other is not null && other.Id != existing.Id) throw new InvalidOperationException("Slug já está em uso.");

        var updated = existing with
        {
            Name = name,
            IsActive = command.IsActive,
            Slug = slug,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await _categories.UpdateAsync(updated, ct);
        return new CategoryResult(updated.Id, updated.TenantId, updated.Code, updated.Slug, updated.Name, updated.IsActive);
    }
}
