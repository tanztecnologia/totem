using TotemAPI.Features.Dashboard.Application.Abstractions;

namespace TotemAPI.Features.Dashboard.Application.UseCases;

public sealed class ListDashboardOrders
{
    public ListDashboardOrders(IDashboardRepository repo)
    {
        _repo = repo;
    }

    private readonly IDashboardRepository _repo;

    public async Task<DashboardOrdersPageResult> HandleAsync(ListDashboardOrdersQuery query, CancellationToken ct)
    {
        if (query.TenantId == Guid.Empty) throw new ArgumentException("Tenant inválido.");
        if (query.Limit <= 0) throw new ArgumentException("Limit inválido.");

        var page = await _repo.GetOrdersPageAsync(
            tenantId: query.TenantId,
            limit: query.Limit,
            cursorUpdatedAt: query.CursorUpdatedAt,
            cursorOrderId: query.CursorOrderId,
            ct: ct
        );

        var items = page.Items
            .Select(
                x =>
                    new DashboardOrderListItem(
                        OrderId: x.OrderId,
                        Comanda: x.Comanda,
                        Status: x.Status,
                        KitchenStatus: x.KitchenStatus,
                        TotalCents: x.TotalCents,
                        CreatedAt: x.CreatedAt,
                        UpdatedAt: x.UpdatedAt,
                        PaymentStatus: x.PaymentStatus,
                        PaymentMethod: x.PaymentMethod,
                        PaymentAmountCents: x.PaymentAmountCents,
                        PaymentProvider: x.PaymentProvider
                    )
            )
            .ToList()
            .AsReadOnly();

        return new DashboardOrdersPageResult(items, page.NextCursorUpdatedAt, page.NextCursorOrderId);
    }
}

public sealed record ListDashboardOrdersQuery(Guid TenantId, int Limit, DateTimeOffset? CursorUpdatedAt, Guid? CursorOrderId);

public sealed record DashboardOrderListItem(
    Guid OrderId,
    string? Comanda,
    TotemAPI.Features.Checkout.Domain.OrderStatus Status,
    TotemAPI.Features.Checkout.Domain.OrderKitchenStatus KitchenStatus,
    int TotalCents,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    TotemAPI.Features.Checkout.Domain.PaymentStatus? PaymentStatus,
    TotemAPI.Features.Checkout.Domain.PaymentMethod? PaymentMethod,
    int? PaymentAmountCents,
    string? PaymentProvider
);

public sealed record DashboardOrdersPageResult(
    IReadOnlyList<DashboardOrderListItem> Items,
    DateTimeOffset? NextCursorUpdatedAt,
    Guid? NextCursorOrderId
);
