using TotemAPI.Features.Cart.Application.Abstractions;
using TotemAPI.Features.Cart.Domain;
using TotemAPI.Features.Catalog.Application.Abstractions;

namespace TotemAPI.Features.Cart.Application.UseCases;

public sealed record GetCartQuery(
    Guid TenantId,
    Guid CartId
);

public sealed record CartItemResult(
    Guid SkuId,
    string Code,
    string Name,
    int UnitPriceCents,
    int Quantity,
    int TotalCents
);

public sealed record GetCartResult(
    Guid Id,
    int TotalCents,
    IReadOnlyList<CartItemResult> Items,
    DateTimeOffset UpdatedAt
);

public sealed class GetCart
{
    public GetCart(ICartRepository carts, ISkuRepository skus)
    {
        _carts = carts;
        _skus = skus;
    }

    private readonly ICartRepository _carts;
    private readonly ISkuRepository _skus;

    public async Task<GetCartResult?> HandleAsync(GetCartQuery query, CancellationToken ct)
    {
        if (query.TenantId == Guid.Empty) throw new ArgumentException("TenantId inválido.");
        if (query.CartId == Guid.Empty) throw new ArgumentException("CartId inválido.");

        var cart = await _carts.GetAsync(query.TenantId, query.CartId, ct);
        if (cart is null) return null;

        var items = await _carts.ListItemsAsync(query.TenantId, query.CartId, ct);
        var results = new List<CartItemResult>(items.Count);
        var totalCents = 0;

        foreach (var item in items.OrderBy(x => x.SkuId))
        {
            var sku = await _skus.GetByIdAsync(query.TenantId, item.SkuId, ct);
            if (sku is null) throw new InvalidOperationException("SKU não encontrado.");
            if (!sku.IsActive) throw new InvalidOperationException("SKU inativo.");

            checked
            {
                var lineTotal = sku.PriceCents * item.Quantity;
                totalCents += lineTotal;
                results.Add(new CartItemResult(sku.Id, sku.Code, sku.Name, sku.PriceCents, item.Quantity, lineTotal));
            }
        }

        return new GetCartResult(
            Id: cart.Id,
            TotalCents: totalCents,
            Items: results,
            UpdatedAt: cart.UpdatedAt
        );
    }
}

