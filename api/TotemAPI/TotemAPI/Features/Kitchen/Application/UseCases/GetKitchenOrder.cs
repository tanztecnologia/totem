using TotemAPI.Features.Catalog.Application.Abstractions;
using TotemAPI.Features.Checkout.Application.Abstractions;
using TotemAPI.Features.Checkout.Domain;
using TotemAPI.Features.Kitchen.Application.Abstractions;

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
    string? Comanda,
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
    public GetKitchenOrder(ICheckoutRepository checkout, ISkuRepository skus, IKitchenSlaRepository slas)
    {
        _checkout = checkout;
        _skus = skus;
        _slas = slas;
    }

    private readonly ICheckoutRepository _checkout;
    private readonly ISkuRepository _skus;
    private readonly IKitchenSlaRepository _slas;

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
        var sla = await LoadSlaOrDefaultAsync(query.TenantId, ct);
        var skus = await _skus.ListAsync(query.TenantId, ct);
        var averagePrepSecondsBySkuId = skus
            .Where(x => x.AveragePrepSeconds is not null && x.AveragePrepSeconds > 0)
            .ToDictionary(x => x.Id, x => x.AveragePrepSeconds!.Value);

        var (elapsedSeconds, targetSeconds, isOverdue) = GetStageMetrics(order, items, averagePrepSecondsBySkuId, sla, now);

        return new GetKitchenOrderResult(
            OrderId: order.Id,
            OrderStatus: order.Status,
            KitchenStatus: order.KitchenStatus,
            Fulfillment: order.Fulfillment,
            TotalCents: order.TotalCents,
            Comanda: order.Comanda,
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

    private static (int elapsedSeconds, int targetSeconds, bool isOverdue) GetStageMetrics(
        Order order,
        IReadOnlyList<OrderItem> items,
        IReadOnlyDictionary<Guid, int> averagePrepSecondsBySkuId,
        KitchenSlaResult sla,
        DateTimeOffset now
    )
    {
        var targetSeconds = GetStageTargetSeconds(order, items, averagePrepSecondsBySkuId, sla);

        var (startAt, targetAt) = order.KitchenStatus switch
        {
            OrderKitchenStatus.Queued => (order.QueuedAt ?? order.UpdatedAt, targetSeconds),
            OrderKitchenStatus.InPreparation => (order.InPreparationAt ?? order.UpdatedAt, targetSeconds),
            OrderKitchenStatus.Ready => (order.ReadyAt ?? order.UpdatedAt, targetSeconds),
            _ => (order.UpdatedAt, 0),
        };

        var elapsed = now - startAt;
        var elapsedSeconds = (int)Math.Max(0, elapsed.TotalSeconds);
        var isOverdue = targetAt > 0 && elapsedSeconds > targetAt;
        return (elapsedSeconds, targetAt, isOverdue);
    }

    private static int GetStageTargetSeconds(
        Order order,
        IReadOnlyList<OrderItem> items,
        IReadOnlyDictionary<Guid, int> averagePrepSecondsBySkuId,
        KitchenSlaResult sla
    )
    {
        return order.KitchenStatus switch
        {
            OrderKitchenStatus.Queued => sla.QueuedTargetSeconds,
            OrderKitchenStatus.Ready => sla.ReadyTargetSeconds,
            OrderKitchenStatus.InPreparation => Math.Max(sla.PreparationBaseTargetSeconds, ComputeSkuBasedPreparationSeconds(items, averagePrepSecondsBySkuId)),
            _ => 0,
        };
    }

    private static int ComputeSkuBasedPreparationSeconds(IReadOnlyList<OrderItem> items, IReadOnlyDictionary<Guid, int> averagePrepSecondsBySkuId)
    {
        var total = 0;
        foreach (var item in items)
        {
            if (item.Quantity <= 0) continue;
            if (!averagePrepSecondsBySkuId.TryGetValue(item.SkuId, out var perUnitSeconds)) continue;
            if (perUnitSeconds <= 0) continue;

            try
            {
                checked
                {
                    total += perUnitSeconds * item.Quantity;
                }
            }
            catch (OverflowException)
            {
                return int.MaxValue;
            }
        }

        return total;
    }

    private async Task<KitchenSlaResult> LoadSlaOrDefaultAsync(Guid tenantId, CancellationToken ct)
    {
        var existing = await _slas.GetAsync(tenantId, ct);
        if (existing is null) return GetKitchenSla.Defaults();
        return new KitchenSlaResult(existing.QueuedTargetSeconds, existing.PreparationBaseTargetSeconds, existing.ReadyTargetSeconds, existing.UpdatedAt);
    }
}
