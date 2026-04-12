using TotemAPI.Features.Cart.Application.Abstractions;
using TotemAPI.Features.Cart.Domain;
using TotemAPI.Features.Catalog.Application.Abstractions;

namespace TotemAPI.Features.Cart.Application.UseCases;

public sealed record SetCartItemCommand(
    Guid TenantId,
    Guid CartId,
    Guid SkuId,
    int Quantity
);

public sealed class SetCartItem
{
    public SetCartItem(ICartRepository carts, ISkuRepository skus)
    {
        _carts = carts;
        _skus = skus;
    }

    private readonly ICartRepository _carts;
    private readonly ISkuRepository _skus;

    public async Task<bool?> HandleAsync(SetCartItemCommand command, CancellationToken ct)
    {
        if (command.TenantId == Guid.Empty) throw new ArgumentException("TenantId inválido.");
        if (command.CartId == Guid.Empty) throw new ArgumentException("CartId inválido.");
        if (command.SkuId == Guid.Empty) throw new ArgumentException("SkuId inválido.");
        if (command.Quantity < 0) throw new ArgumentException("Quantity inválido.");

        var cart = await _carts.GetAsync(command.TenantId, command.CartId, ct);
        if (cart is null) return null;

        if (command.Quantity == 0)
        {
            await _carts.RemoveItemAsync(command.TenantId, command.CartId, command.SkuId, ct);
            await _carts.TouchAsync(command.TenantId, command.CartId, DateTimeOffset.UtcNow, ct);
            return true;
        }

        var sku = await _skus.GetByIdAsync(command.TenantId, command.SkuId, ct);
        if (sku is null) throw new InvalidOperationException("SKU não encontrado.");
        if (!sku.IsActive) throw new InvalidOperationException("SKU inativo.");

        var now = DateTimeOffset.UtcNow;
        var existing = await _carts.GetItemAsync(command.TenantId, command.CartId, command.SkuId, ct);

        var item = existing is null
            ? new ShoppingCartItem(Guid.NewGuid(), command.TenantId, command.CartId, command.SkuId, command.Quantity, now, now)
            : existing with { Quantity = command.Quantity, UpdatedAt = now };

        await _carts.UpsertItemAsync(item, ct);
        await _carts.TouchAsync(command.TenantId, command.CartId, now, ct);
        return true;
    }
}

