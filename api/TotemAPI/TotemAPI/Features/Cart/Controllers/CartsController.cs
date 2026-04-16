using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TotemAPI.Features.Cart.Application.UseCases;
using TotemAPI.Features.Identity.Domain;
using TotemAPI.Infrastructure.Auth;

namespace TotemAPI.Features.Cart.Controllers;

[ApiController]
[Route("api/carts")]
[Authorize]
public sealed class CartsController : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<CreateCartResult>> Create(
        [FromServices] CreateCart createCart,
        CancellationToken ct
    )
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        if (!CanReadCart()) return Forbid();

        try
        {
            var result = await createCart.HandleAsync(new CreateCartCommand(tenantId), ct);
            return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GetCartResult>> Get(
        [FromServices] GetCart getCart,
        [FromRoute] Guid id,
        CancellationToken ct
    )
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        if (!CanReadCart()) return Forbid();

        try
        {
            var result = await getCart.HandleAsync(new GetCartQuery(tenantId, id), ct);
            return result is null ? NotFound() : Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpPut("{id:guid}/items/{skuId:guid}")]
    public async Task<IActionResult> SetItem(
        [FromServices] SetCartItem setCartItem,
        [FromRoute] Guid id,
        [FromRoute] Guid skuId,
        [FromBody] SetCartItemRequest request,
        CancellationToken ct
    )
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        if (!CanReadCart()) return Forbid();

        try
        {
            var ok = await setCartItem.HandleAsync(new SetCartItemCommand(tenantId, id, skuId, request.Quantity), ct);
            return ok is null ? NotFound() : NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpPost("{id:guid}/items")]
    public async Task<IActionResult> AddItem(
        [FromServices] SetCartItem setCartItem,
        [FromRoute] Guid id,
        [FromBody] AddCartItemRequest request,
        CancellationToken ct
    )
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        if (!CanReadCart()) return Forbid();

        try
        {
            var ok = await setCartItem.HandleAsync(
                new SetCartItemCommand(tenantId, id, request.SkuId, request.Quantity),
                ct
            );
            return ok is null ? NotFound() : NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpDelete("{id:guid}/items/{skuId:guid}")]
    public async Task<IActionResult> RemoveItem(
        [FromServices] SetCartItem setCartItem,
        [FromRoute] Guid id,
        [FromRoute] Guid skuId,
        CancellationToken ct
    )
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        if (!CanReadCart()) return Forbid();

        try
        {
            var ok = await setCartItem.HandleAsync(new SetCartItemCommand(tenantId, id, skuId, 0), ct);
            return ok is null ? NotFound() : NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{id:guid}/items")]
    public async Task<IActionResult> Clear(
        [FromServices] ClearCart clearCart,
        [FromRoute] Guid id,
        CancellationToken ct
    )
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        if (!CanReadCart()) return Forbid();

        try
        {
            var ok = await clearCart.HandleAsync(new ClearCartCommand(tenantId, id), ct);
            return ok is null ? NotFound() : NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    private bool CanReadCart()
    {
        return User.HasPermission(Permissions.CartRead);
    }

    private bool TryGetTenantId(out Guid tenantId)
    {
        tenantId = default;
        var raw = User.FindFirstValue("tenant_id");
        return Guid.TryParse(raw, out tenantId);
    }
}

public sealed record SetCartItemRequest(int Quantity);

public sealed record AddCartItemRequest(Guid SkuId, int Quantity);
