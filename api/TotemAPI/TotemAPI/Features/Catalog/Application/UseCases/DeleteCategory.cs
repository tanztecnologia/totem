using TotemAPI.Features.Catalog.Application.Abstractions;
using TotemAPI.Infrastructure.Persistence;

namespace TotemAPI.Features.Catalog.Application.UseCases;

public sealed record DeleteCategoryCommand(Guid TenantId, string Code);

public sealed class DeleteCategory
{
    public DeleteCategory(ICategoryRepository categories)
    {
        _categories = categories;
    }

    private readonly ICategoryRepository _categories;

    public async Task<bool> HandleAsync(DeleteCategoryCommand command, CancellationToken ct)
    {
        if (command.TenantId == Guid.Empty) throw new ArgumentException("TenantId inválido.");
        var code = CategoryMapping.NormalizeNumericCode(command.Code);
        if (code.Length < 2) throw new ArgumentException("Code inválido.");

        var existing = await _categories.GetByCodeAsync(command.TenantId, code, ct);
        if (existing is null) return false;

        await _categories.DeleteByCodeAsync(command.TenantId, code, ct);
        return true;
    }
}
