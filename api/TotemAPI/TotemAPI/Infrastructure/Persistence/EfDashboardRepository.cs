using Microsoft.EntityFrameworkCore;
using TotemAPI.Features.Checkout.Domain;
using TotemAPI.Features.Dashboard.Application.Abstractions;

namespace TotemAPI.Infrastructure.Persistence;

public sealed class EfDashboardRepository : IDashboardRepository
{
    public EfDashboardRepository(TotemDbContext db)
    {
        _db = db;
    }

    private readonly TotemDbContext _db;

    public async Task<DashboardOverviewSnapshot> GetOverviewAsync(
        Guid tenantId,
        DateTimeOffset fromInclusive,
        DateTimeOffset toInclusive,
        CancellationToken ct
    )
    {
        var orders = await _db.Orders
            .Where(x => x.TenantId == tenantId && x.CreatedAt >= fromInclusive && x.CreatedAt <= toInclusive)
            .AsNoTracking()
            .ToListAsync(ct);

        var ordersCount = orders.Count;
        var paidOrdersCount = orders.Count(x => x.Status == OrderStatus.Paid);
        var cancelledOrdersCount = orders.Count(x => x.Status == OrderStatus.Cancelled);

        var ordersByKitchenStatus = orders
            .GroupBy(x => x.KitchenStatus)
            .Select(g => new DashboardKitchenStatusTotal(g.Key, g.Count()))
            .ToList()
            .AsReadOnly();

        var payments = await _db.Payments
            .Where(
                x =>
                    x.TenantId == tenantId
                    && x.Status == PaymentStatus.Approved
                    && x.UpdatedAt >= fromInclusive
                    && x.UpdatedAt <= toInclusive
            )
            .AsNoTracking()
            .ToListAsync(ct);

        var paymentsByMethod = payments
            .GroupBy(x => x.Method)
            .Select(g => new DashboardPaymentMethodTotal(g.Key, g.Sum(x => x.AmountCents), g.Count()))
            .ToList()
            .AsReadOnly();

        var paymentsByProvider = payments
            .GroupBy(x => x.Provider)
            .Select(g => new DashboardPaymentProviderTotal(g.Key, g.Sum(x => x.AmountCents), g.Count()))
            .ToList()
            .AsReadOnly();

        var revenueCents = paymentsByMethod.Sum(x => x.AmountCents);

        return new DashboardOverviewSnapshot(
            OrdersCount: ordersCount,
            PaidOrdersCount: paidOrdersCount,
            CancelledOrdersCount: cancelledOrdersCount,
            RevenueCents: revenueCents,
            PaymentsByMethod: paymentsByMethod,
            PaymentsByProvider: paymentsByProvider,
            OrdersByKitchenStatus: ordersByKitchenStatus
        );
    }

    public async Task<DashboardOrdersPageSnapshot> GetOrdersPageAsync(
        Guid tenantId,
        int limit,
        DateTimeOffset? cursorUpdatedAt,
        Guid? cursorOrderId,
        CancellationToken ct
    )
    {
        if (limit <= 0) return new DashboardOrdersPageSnapshot(Array.Empty<DashboardOrderSnapshot>(), null, null);
        if (limit > 200) limit = 200;

        List<OrderRow> orders;
        if (cursorUpdatedAt is not null && cursorOrderId is not null && cursorOrderId != Guid.Empty)
        {
            orders = await _db.Orders
                .FromSqlRaw(
                    "SELECT * FROM orders WHERE TenantId = {0} AND (UpdatedAt < {1} OR (UpdatedAt = {1} AND Id < {2})) ORDER BY UpdatedAt DESC, Id DESC LIMIT {3}",
                    tenantId,
                    cursorUpdatedAt.Value,
                    cursorOrderId.Value,
                    limit + 1
                )
                .AsNoTracking()
                .ToListAsync(ct);
        }
        else
        {
            orders = await _db.Orders
                .FromSqlRaw("SELECT * FROM orders WHERE TenantId = {0} ORDER BY UpdatedAt DESC, Id DESC LIMIT {1}", tenantId, limit + 1)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        if (orders.Count == 0) return new DashboardOrdersPageSnapshot(Array.Empty<DashboardOrderSnapshot>(), null, null);

        DateTimeOffset? nextCursorUpdatedAt = null;
        Guid? nextCursorOrderId = null;

        if (orders.Count > limit)
        {
            var last = orders[limit - 1];
            nextCursorUpdatedAt = last.UpdatedAt;
            nextCursorOrderId = last.Id;
            orders = orders.Take(limit).ToList();
        }

        var orderIds = orders.Select(x => x.Id).ToList();

        var payments = await _db.Payments
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && orderIds.Contains(x.OrderId))
            .ToListAsync(ct);

        var paymentDict = payments.GroupBy(x => x.OrderId).ToDictionary(g => g.Key, g => g.First());

        var result = new List<DashboardOrderSnapshot>(orders.Count);
        foreach (var o in orders)
        {
            paymentDict.TryGetValue(o.Id, out var p);
            result.Add(new DashboardOrderSnapshot(
                o.Id,
                o.Comanda,
                o.Status,
                o.KitchenStatus,
                o.TotalCents,
                o.CreatedAt,
                o.UpdatedAt,
                p?.Status,
                p?.Method,
                p?.AmountCents,
                p?.Provider
            ));
        }

        return new DashboardOrdersPageSnapshot(result.AsReadOnly(), nextCursorUpdatedAt, nextCursorOrderId);
    }
}
