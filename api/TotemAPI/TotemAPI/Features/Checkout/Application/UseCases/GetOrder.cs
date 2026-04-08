using TotemAPI.Features.Checkout.Application.Abstractions;
using TotemAPI.Features.Checkout.Domain;

namespace TotemAPI.Features.Checkout.Application.UseCases;

public sealed record GetOrderQuery(
    Guid TenantId,
    Guid OrderId
);

public sealed record GetOrderResult(
    Guid Id,
    Guid TenantId,
    OrderFulfillment Fulfillment,
    int TotalCents,
    OrderStatus Status,
    DateTimeOffset CreatedAt,
    IReadOnlyList<CheckoutOrderItemResult> Items
);

public sealed class GetOrder
{
    public GetOrder(ICheckoutRepository checkout)
    {
        _checkout = checkout;
    }

    private readonly ICheckoutRepository _checkout;

    public async Task<GetOrderResult?> HandleAsync(GetOrderQuery query, CancellationToken ct)
    {
        if (query.TenantId == Guid.Empty) throw new ArgumentException("TenantId inválido.");
        if (query.OrderId == Guid.Empty) throw new ArgumentException("OrderId inválido.");

        var order = await _checkout.GetOrderAsync(query.TenantId, query.OrderId, ct);
        if (order is null) return null;

        var items = await _checkout.ListOrderItemsAsync(query.TenantId, query.OrderId, ct);
        var resultItems = items
            .OrderBy(x => x.SkuName)
            .Select(x => new CheckoutOrderItemResult(x.SkuId, x.SkuCode, x.SkuName, x.UnitPriceCents, x.Quantity, x.TotalCents))
            .ToList();

        return new GetOrderResult(
            Id: order.Id,
            TenantId: order.TenantId,
            Fulfillment: order.Fulfillment,
            TotalCents: order.TotalCents,
            Status: order.Status,
            CreatedAt: order.CreatedAt,
            Items: resultItems
        );
    }
}
