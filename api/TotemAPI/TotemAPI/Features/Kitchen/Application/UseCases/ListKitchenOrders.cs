using TotemAPI.Features.Checkout.Application.Abstractions;
using TotemAPI.Features.Checkout.Domain;

namespace TotemAPI.Features.Kitchen.Application.UseCases;

public sealed record ListKitchenOrdersQuery(
    Guid TenantId,
    IReadOnlyList<OrderKitchenStatus>? KitchenStatuses,
    int Limit
);

public sealed record KitchenOrderItemResult(
    Guid SkuId,
    string Code,
    string Name,
    int Quantity
);

public sealed record KitchenOrderResult(
    Guid OrderId,
    OrderStatus OrderStatus,
    OrderKitchenStatus KitchenStatus,
    OrderFulfillment Fulfillment,
    int TotalCents,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? QueuedAt,
    DateTimeOffset? InPreparationAt,
    DateTimeOffset? ReadyAt,
    DateTimeOffset? CompletedAt,
    DateTimeOffset? CancelledAt,
    int CurrentStageElapsedSeconds,
    int CurrentStageTargetSeconds,
    bool IsOverdue,
    IReadOnlyList<KitchenOrderItemResult> Items
);

public sealed class ListKitchenOrders
{
    public ListKitchenOrders(ICheckoutRepository checkout)
    {
        _checkout = checkout;
    }

    private readonly ICheckoutRepository _checkout;

    public async Task<IReadOnlyList<KitchenOrderResult>> HandleAsync(ListKitchenOrdersQuery query, CancellationToken ct)
    {
        if (query.TenantId == Guid.Empty) throw new ArgumentException("TenantId inválido.");
        if (query.Limit <= 0) throw new ArgumentException("Limit inválido.");

        var now = DateTimeOffset.UtcNow;
        var orders = await _checkout.ListOrdersAsync(query.TenantId, query.KitchenStatuses, query.Limit, ct);
        if (orders.Count == 0) return Array.Empty<KitchenOrderResult>();

        var ordered = orders
            .OrderByDescending(x => Score(x, now))
            .ThenByDescending(x => x.CreatedAt)
            .ToList();

        var results = new List<KitchenOrderResult>(orders.Count);
        foreach (var order in ordered)
        {
            var (elapsedSeconds, targetSeconds, isOverdue) = GetStageMetrics(order, now);

            var items = await _checkout.ListOrderItemsAsync(query.TenantId, order.Id, ct);
            var mappedItems = items
                .OrderBy(x => x.SkuName)
                .Select(x => new KitchenOrderItemResult(x.SkuId, x.SkuCode, x.SkuName, x.Quantity))
                .ToList();

            results.Add(
                new KitchenOrderResult(
                    OrderId: order.Id,
                    OrderStatus: order.Status,
                    KitchenStatus: order.KitchenStatus,
                    Fulfillment: order.Fulfillment,
                    TotalCents: order.TotalCents,
                    CreatedAt: order.CreatedAt,
                    UpdatedAt: order.UpdatedAt,
                    QueuedAt: order.QueuedAt,
                    InPreparationAt: order.InPreparationAt,
                    ReadyAt: order.ReadyAt,
                    CompletedAt: order.CompletedAt,
                    CancelledAt: order.CancelledAt,
                    CurrentStageElapsedSeconds: elapsedSeconds,
                    CurrentStageTargetSeconds: targetSeconds,
                    IsOverdue: isOverdue,
                    Items: mappedItems
                )
            );
        }

        return results;
    }

    private static int Score(Order order, DateTimeOffset now)
    {
        var (elapsedSeconds, targetSeconds, isOverdue) = GetStageMetrics(order, now);
        if (targetSeconds <= 0) return 0;

        if (isOverdue)
        {
            var overdueBy = Math.Max(0, elapsedSeconds - targetSeconds);
            return 1_000_000 + overdueBy;
        }

        var remaining = Math.Max(0, targetSeconds - elapsedSeconds);
        var warningWindow = Math.Max(1, targetSeconds / 4);
        if (remaining <= warningWindow)
        {
            return 500_000 + (warningWindow - remaining);
        }

        return elapsedSeconds;
    }

    private static (int elapsedSeconds, int targetSeconds, bool isOverdue) GetStageMetrics(Order order, DateTimeOffset now)
    {
        var (startAt, targetSeconds) = order.KitchenStatus switch
        {
            OrderKitchenStatus.Queued => (order.QueuedAt ?? order.UpdatedAt, 120),
            OrderKitchenStatus.InPreparation => (order.InPreparationAt ?? order.UpdatedAt, 480),
            OrderKitchenStatus.Ready => (order.ReadyAt ?? order.UpdatedAt, 120),
            _ => (order.UpdatedAt, 0),
        };

        var elapsed = now - startAt;
        var elapsedSeconds = (int)Math.Max(0, elapsed.TotalSeconds);
        var isOverdue = targetSeconds > 0 && elapsedSeconds > targetSeconds;
        return (elapsedSeconds, targetSeconds, isOverdue);
    }
}
