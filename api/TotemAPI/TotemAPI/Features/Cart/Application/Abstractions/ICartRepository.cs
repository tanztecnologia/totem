using TotemAPI.Features.Cart.Domain;

namespace TotemAPI.Features.Cart.Application.Abstractions;

public interface ICartRepository
{
    Task CreateAsync(ShoppingCart cart, CancellationToken ct);
    Task<ShoppingCart?> GetAsync(Guid tenantId, Guid cartId, CancellationToken ct);
    Task<IReadOnlyList<ShoppingCartItem>> ListItemsAsync(Guid tenantId, Guid cartId, CancellationToken ct);
    Task<ShoppingCartItem?> GetItemAsync(Guid tenantId, Guid cartId, Guid skuId, CancellationToken ct);
    Task UpsertItemAsync(ShoppingCartItem item, CancellationToken ct);
    Task RemoveItemAsync(Guid tenantId, Guid cartId, Guid skuId, CancellationToken ct);
    Task ClearAsync(Guid tenantId, Guid cartId, CancellationToken ct);
    Task TouchAsync(Guid tenantId, Guid cartId, DateTimeOffset updatedAt, CancellationToken ct);
}
