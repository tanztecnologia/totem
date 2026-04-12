using TotemAPI.Features.Cart.Application.Abstractions;

namespace TotemAPI.Features.Cart.Application.UseCases;

public sealed record ClearCartCommand(
    Guid TenantId,
    Guid CartId
);

public sealed class ClearCart
{
    public ClearCart(ICartRepository carts)
    {
        _carts = carts;
    }

    private readonly ICartRepository _carts;

    public async Task<bool?> HandleAsync(ClearCartCommand command, CancellationToken ct)
    {
        if (command.TenantId == Guid.Empty) throw new ArgumentException("TenantId inválido.");
        if (command.CartId == Guid.Empty) throw new ArgumentException("CartId inválido.");

        var cart = await _carts.GetAsync(command.TenantId, command.CartId, ct);
        if (cart is null) return null;

        await _carts.ClearAsync(command.TenantId, command.CartId, ct);
        await _carts.TouchAsync(command.TenantId, command.CartId, DateTimeOffset.UtcNow, ct);
        return true;
    }
}

