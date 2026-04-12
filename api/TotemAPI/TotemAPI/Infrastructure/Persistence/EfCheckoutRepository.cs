using Microsoft.EntityFrameworkCore;
using TotemAPI.Features.Checkout.Application.Abstractions;
using TotemAPI.Features.Checkout.Domain;

namespace TotemAPI.Infrastructure.Persistence;

public sealed class EfCheckoutRepository : ICheckoutRepository
{
    public EfCheckoutRepository(TotemDbContext db)
    {
        _db = db;
    }

    private readonly TotemDbContext _db;

    public async Task CreateAsync(Order order, IReadOnlyList<OrderItem> items, Payment payment, CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        _db.Orders.Add(
            new OrderRow
            {
                Id = order.Id,
                TenantId = order.TenantId,
                CartId = order.CartId,
                Fulfillment = order.Fulfillment,
                TotalCents = order.TotalCents,
                Status = order.Status,
                KitchenStatus = order.KitchenStatus,
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt,
                QueuedAt = order.QueuedAt,
                InPreparationAt = order.InPreparationAt,
                ReadyAt = order.ReadyAt,
                CompletedAt = order.CompletedAt,
                CancelledAt = order.CancelledAt,
            }
        );

        foreach (var item in items)
        {
            _db.OrderItems.Add(
                new OrderItemRow
                {
                    Id = item.Id,
                    TenantId = item.TenantId,
                    OrderId = item.OrderId,
                    SkuId = item.SkuId,
                    SkuCode = item.SkuCode,
                    SkuName = item.SkuName,
                    UnitPriceCents = item.UnitPriceCents,
                    Quantity = item.Quantity,
                    TotalCents = item.TotalCents,
                    CreatedAt = item.CreatedAt,
                }
            );
        }

        _db.Payments.Add(
            new PaymentRow
            {
                Id = payment.Id,
                TenantId = payment.TenantId,
                OrderId = payment.OrderId,
                Method = payment.Method,
                Status = payment.Status,
                AmountCents = payment.AmountCents,
                Provider = payment.Provider,
                ProviderReference = payment.ProviderReference,
                TransactionId = payment.TransactionId,
                PixPayload = payment.PixPayload,
                PixExpiresAt = payment.PixExpiresAt,
                CreatedAt = payment.CreatedAt,
                UpdatedAt = payment.UpdatedAt,
            }
        );

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    public async Task<Order?> GetOrderAsync(Guid tenantId, Guid orderId, CancellationToken ct)
    {
        var row = await _db.Orders.AsNoTracking().SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Id == orderId, ct);
        return row?.ToDomain();
    }

    public async Task<IReadOnlyList<Order>> ListOrdersAsync(
        Guid tenantId,
        IReadOnlyList<OrderKitchenStatus>? kitchenStatuses,
        int limit,
        CancellationToken ct
    )
    {
        if (limit <= 0) return Array.Empty<Order>();
        if (limit > 200) limit = 200;

        var query = _db.Orders.AsNoTracking().Where(x => x.TenantId == tenantId);
        if (kitchenStatuses is { Count: > 0 })
        {
            query = query.Where(x => kitchenStatuses.Contains(x.KitchenStatus));
        }

        var list = await query.ToListAsync(ct);
        return list
            .OrderByDescending(x => x.UpdatedAt)
            .Take(limit)
            .Select(x => x.ToDomain())
            .ToList()
            .AsReadOnly();
    }

    public async Task<IReadOnlyList<OrderItem>> ListOrderItemsAsync(Guid tenantId, Guid orderId, CancellationToken ct)
    {
        return await _db.OrderItems
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.OrderId == orderId)
            .OrderBy(x => x.SkuName)
            .Select(x => x.ToDomain())
            .ToListAsync(ct);
    }

    public async Task<Payment?> GetPaymentAsync(Guid tenantId, Guid paymentId, CancellationToken ct)
    {
        var row = await _db.Payments.AsNoTracking().SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Id == paymentId, ct);
        return row?.ToDomain();
    }

    public async Task UpdatePaymentAsync(Payment payment, CancellationToken ct)
    {
        var row = await _db.Payments.SingleOrDefaultAsync(x => x.TenantId == payment.TenantId && x.Id == payment.Id, ct);
        if (row is null) throw new InvalidOperationException("Pagamento não encontrado.");

        row.Status = payment.Status;
        row.TransactionId = payment.TransactionId;
        row.UpdatedAt = payment.UpdatedAt;

        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateOrderAsync(Order order, CancellationToken ct)
    {
        var row = await _db.Orders.SingleOrDefaultAsync(x => x.TenantId == order.TenantId && x.Id == order.Id, ct);
        if (row is null) throw new InvalidOperationException("Pedido não encontrado.");

        row.CartId = order.CartId;
        row.Status = order.Status;
        row.KitchenStatus = order.KitchenStatus;
        row.UpdatedAt = order.UpdatedAt;
        row.QueuedAt = order.QueuedAt;
        row.InPreparationAt = order.InPreparationAt;
        row.ReadyAt = order.ReadyAt;
        row.CompletedAt = order.CompletedAt;
        row.CancelledAt = order.CancelledAt;
        await _db.SaveChangesAsync(ct);
    }
}
