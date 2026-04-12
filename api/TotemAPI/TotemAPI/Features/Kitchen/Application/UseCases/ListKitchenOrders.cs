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

        var orders = await _checkout.ListOrdersAsync(query.TenantId, query.KitchenStatuses, query.Limit, ct);
        if (orders.Count == 0) return Array.Empty<KitchenOrderResult>();

        var results = new List<KitchenOrderResult>(orders.Count);
        foreach (var order in orders)
        {
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
                    Items: mappedItems
                )
            );
        }

        return results;
    }
}

