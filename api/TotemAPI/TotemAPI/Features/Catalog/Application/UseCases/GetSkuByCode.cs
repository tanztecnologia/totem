using TotemAPI.Features.Catalog.Application.Abstractions;

namespace TotemAPI.Features.Catalog.Application.UseCases;

public sealed record GetSkuByCodeQuery(Guid TenantId, string Code);

public sealed class GetSkuByCode
{
    public GetSkuByCode(ISkuRepository skus)
    {
        _skus = skus;
    }

    private readonly ISkuRepository _skus;

    public async Task<SkuResult?> HandleAsync(GetSkuByCodeQuery query, CancellationToken ct)
    {
        if (query.TenantId == Guid.Empty) throw new ArgumentException("TenantId inválido.");

        var code = (query.Code ?? string.Empty).Trim();
        if (code.Length < 2) throw new ArgumentException("Code inválido.");

        var sku = await _skus.GetByCodeAsync(query.TenantId, code, ct);
        if (sku is null) return null;

        return new SkuResult(
            Id: sku.Id,
            TenantId: sku.TenantId,
            CategoryCode: sku.CategoryCode,
            Code: sku.Code,
            Name: sku.Name,
            PriceCents: sku.PriceCents,
            AveragePrepSeconds: sku.AveragePrepSeconds,
            ImageUrl: sku.ImageUrl,
            IsActive: sku.IsActive
        );
    }
}
