using Microsoft.EntityFrameworkCore;
using TotemAPI.Features.Cart.Application.Abstractions;
using TotemAPI.Features.Cart.Domain;

namespace TotemAPI.Infrastructure.Persistence;

public sealed class EfCartRepository : ICartRepository
{
    public EfCartRepository(TotemDbContext db)
    {
        _db = db;
    }

    private readonly TotemDbContext _db;

    public async Task CreateAsync(ShoppingCart cart, CancellationToken ct)
    {
        _db.Carts.Add(
            new CartRow
            {
                Id = cart.Id,
                TenantId = cart.TenantId,
                CreatedAt = cart.CreatedAt,
                UpdatedAt = cart.UpdatedAt,
            }
        );

        await _db.SaveChangesAsync(ct);
    }

    public async Task<ShoppingCart?> GetAsync(Guid tenantId, Guid cartId, CancellationToken ct)
    {
        var row = await _db.Carts.AsNoTracking().FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == cartId, ct);
        return row?.ToDomain();
    }

    public async Task<IReadOnlyList<ShoppingCartItem>> ListItemsAsync(Guid tenantId, Guid cartId, CancellationToken ct)
    {
        var rows = await _db.CartItems
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.CartId == cartId)
            .OrderBy(x => x.SkuId)
            .ToListAsync(ct);

        return rows.Select(x => x.ToDomain()).ToList();
    }

    public async Task<ShoppingCartItem?> GetItemAsync(Guid tenantId, Guid cartId, Guid skuId, CancellationToken ct)
    {
        var row = await _db.CartItems.AsNoTracking().FirstOrDefaultAsync(
            x => x.TenantId == tenantId && x.CartId == cartId && x.SkuId == skuId,
            ct
        );
        return row?.ToDomain();
    }

    public async Task UpsertItemAsync(ShoppingCartItem item, CancellationToken ct)
    {
        var existing = await _db.CartItems.FirstOrDefaultAsync(
            x => x.TenantId == item.TenantId && x.CartId == item.CartId && x.SkuId == item.SkuId,
            ct
        );

        if (existing is null)
        {
            _db.CartItems.Add(
                new CartItemRow
                {
                    Id = item.Id,
                    TenantId = item.TenantId,
                    CartId = item.CartId,
                    SkuId = item.SkuId,
                    Quantity = item.Quantity,
                    CreatedAt = item.CreatedAt,
                    UpdatedAt = item.UpdatedAt,
                }
            );
        }
        else
        {
            existing.Quantity = item.Quantity;
            existing.UpdatedAt = item.UpdatedAt;
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task RemoveItemAsync(Guid tenantId, Guid cartId, Guid skuId, CancellationToken ct)
    {
        var row = await _db.CartItems.FirstOrDefaultAsync(
            x => x.TenantId == tenantId && x.CartId == cartId && x.SkuId == skuId,
            ct
        );
        if (row is null) return;
        _db.CartItems.Remove(row);
        await _db.SaveChangesAsync(ct);
    }

    public async Task ClearAsync(Guid tenantId, Guid cartId, CancellationToken ct)
    {
        var rows = await _db.CartItems.Where(x => x.TenantId == tenantId && x.CartId == cartId).ToListAsync(ct);
        if (rows.Count == 0) return;
        _db.CartItems.RemoveRange(rows);
        await _db.SaveChangesAsync(ct);
    }

    public async Task TouchAsync(Guid tenantId, Guid cartId, DateTimeOffset updatedAt, CancellationToken ct)
    {
        var row = await _db.Carts.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == cartId, ct);
        if (row is null) return;
        row.UpdatedAt = updatedAt;
        await _db.SaveChangesAsync(ct);
    }
}
