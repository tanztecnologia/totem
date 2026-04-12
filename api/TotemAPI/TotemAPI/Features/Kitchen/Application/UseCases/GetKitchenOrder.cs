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

        return new GetKitchenOrderResult(
            OrderId: order.Id,
            OrderStatus: order.Status,
            KitchenStatus: order.KitchenStatus,
            Fulfillment: order.Fulfillment,
            TotalCents: order.TotalCents,
            CreatedAt: order.CreatedAt,
            UpdatedAt: order.UpdatedAt,
            Items: mappedItems
        );
    }
}

