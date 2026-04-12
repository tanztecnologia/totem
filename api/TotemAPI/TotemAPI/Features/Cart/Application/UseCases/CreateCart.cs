using TotemAPI.Features.Cart.Application.Abstractions;
using TotemAPI.Features.Cart.Domain;

namespace TotemAPI.Features.Cart.Application.UseCases;

public sealed record CreateCartCommand(Guid TenantId);

public sealed record CreateCartResult(
    Guid Id,
    DateTimeOffset CreatedAt
);

public sealed class CreateCart
{
    public CreateCart(ICartRepository carts)
    {
        _carts = carts;
    }

    private readonly ICartRepository _carts;

    public async Task<CreateCartResult> HandleAsync(CreateCartCommand command, CancellationToken ct)
    {
        if (command.TenantId == Guid.Empty) throw new ArgumentException("TenantId inválido.");

        var now = DateTimeOffset.UtcNow;
        var cart = new ShoppingCart(
            Id: Guid.NewGuid(),
            TenantId: command.TenantId,
            CreatedAt: now,
            UpdatedAt: now
        );

        await _carts.CreateAsync(cart, ct);
        return new CreateCartResult(cart.Id, cart.CreatedAt);
    }
}

