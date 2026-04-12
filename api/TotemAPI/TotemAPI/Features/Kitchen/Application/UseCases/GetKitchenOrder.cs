using TotemAPI.Features.Checkout.Application.Abstractions;
using TotemAPI.Features.Checkout.Domain;

namespace TotemAPI.Features.Kitchen.Application.UseCases;

public sealed record GetKitchenOrderQuery(
    Guid TenantId,
    Guid OrderId
);

public sealed record GetKitchenOrderResult(
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

public sealed class GetKitchenOrder
{
    public GetKitchenOrder(ICheckoutRepository checkout)
    {
        _checkout = checkout;
    }

    private readonly ICheckoutRepository _checkout;

    public async Task<GetKitchenOrderResult?> HandleAsync(GetKitchenOrderQuery query, CancellationToken ct)
    {
        if (query.TenantId == Guid.Empty) throw new ArgumentException("TenantId inválido.");
        if (query.OrderId == Guid.Empty) throw new ArgumentException("OrderId inválido.");

        var order = await _checkout.GetOrderAsync(query.TenantId, query.OrderId, ct);
        if (order is null) return null;

        var items = await _checkout.ListOrderItemsAsync(query.TenantId, query.OrderId, ct);
        var mappedItems = items
            .OrderBy(x => x.SkuName)
            .Select(x => new KitchenOrderItemResult(x.SkuId, x.SkuCode, x.SkuName, x.Quantity))
            .ToList();

        var now = DateTimeOffset.UtcNow;
        var (elapsedSeconds, targetSeconds, isOverdue) = GetStageMetrics(order, now);

        return new GetKitchenOrderResult(
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
        );
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
