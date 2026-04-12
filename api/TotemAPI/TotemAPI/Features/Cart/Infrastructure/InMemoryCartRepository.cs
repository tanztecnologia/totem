using TotemAPI.Features.Cart.Application.Abstractions;
using TotemAPI.Features.Cart.Domain;

namespace TotemAPI.Features.Cart.Infrastructure;

public sealed class InMemoryCartRepository : ICartRepository
{
    private readonly Dictionary<Guid, ShoppingCart> _carts = new();
    private readonly Dictionary<Guid, List<ShoppingCartItem>> _itemsByCartId = new();

    public Task CreateAsync(ShoppingCart cart, CancellationToken ct)
    {
        _carts[cart.Id] = cart;
        _itemsByCartId.TryAdd(cart.Id, new List<ShoppingCartItem>());
        return Task.CompletedTask;
    }

    public Task<ShoppingCart?> GetAsync(Guid tenantId, Guid cartId, CancellationToken ct)
    {
        if (!_carts.TryGetValue(cartId, out var cart)) return Task.FromResult<ShoppingCart?>(null);
        return Task.FromResult(cart.TenantId == tenantId ? cart : null);
    }

    public Task<IReadOnlyList<ShoppingCartItem>> ListItemsAsync(Guid tenantId, Guid cartId, CancellationToken ct)
    {
        if (!_itemsByCartId.TryGetValue(cartId, out var items)) return Task.FromResult<IReadOnlyList<ShoppingCartItem>>(Array.Empty<ShoppingCartItem>());
        return Task.FromResult<IReadOnlyList<ShoppingCartItem>>(items.Where(x => x.TenantId == tenantId).OrderBy(x => x.SkuId).ToList());
    }

    public Task<ShoppingCartItem?> GetItemAsync(Guid tenantId, Guid cartId, Guid skuId, CancellationToken ct)
    {
        if (!_itemsByCartId.TryGetValue(cartId, out var items)) return Task.FromResult<ShoppingCartItem?>(null);
        var item = items.FirstOrDefault(x => x.TenantId == tenantId && x.SkuId == skuId);
        return Task.FromResult(item);
    }

    public Task UpsertItemAsync(ShoppingCartItem item, CancellationToken ct)
    {
        var list = _itemsByCartId.GetValueOrDefault(item.CartId);
        if (list is null)
        {
            list = new List<ShoppingCartItem>();
            _itemsByCartId[item.CartId] = list;
        }

        var idx = list.FindIndex(x => x.TenantId == item.TenantId && x.SkuId == item.SkuId);
        if (idx >= 0) list[idx] = item;
        else list.Add(item);
        return Task.CompletedTask;
    }

    public Task RemoveItemAsync(Guid tenantId, Guid cartId, Guid skuId, CancellationToken ct)
    {
        if (!_itemsByCartId.TryGetValue(cartId, out var list)) return Task.CompletedTask;
        list.RemoveAll(x => x.TenantId == tenantId && x.SkuId == skuId);
        return Task.CompletedTask;
    }

    public Task ClearAsync(Guid tenantId, Guid cartId, CancellationToken ct)
    {
        if (!_itemsByCartId.TryGetValue(cartId, out var list)) return Task.CompletedTask;
        list.RemoveAll(x => x.TenantId == tenantId);
        return Task.CompletedTask;
    }

    public Task TouchAsync(Guid tenantId, Guid cartId, DateTimeOffset updatedAt, CancellationToken ct)
    {
        if (!_carts.TryGetValue(cartId, out var cart)) return Task.CompletedTask;
        if (cart.TenantId != tenantId) return Task.CompletedTask;
        _carts[cartId] = cart with { UpdatedAt = updatedAt };
        return Task.CompletedTask;
    }
}

