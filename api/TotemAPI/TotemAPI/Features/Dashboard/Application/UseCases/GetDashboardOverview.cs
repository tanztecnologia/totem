using TotemAPI.Features.Checkout.Domain;
using TotemAPI.Features.Dashboard.Application.Abstractions;

namespace TotemAPI.Features.Dashboard.Application.UseCases;

public sealed class GetDashboardOverview
{
    public GetDashboardOverview(IDashboardRepository repo)
    {
        _repo = repo;
    }

    private readonly IDashboardRepository _repo;

    public async Task<DashboardOverviewResult> HandleAsync(GetDashboardOverviewQuery query, CancellationToken ct)
    {
        if (query.TenantId == Guid.Empty) throw new ArgumentException("Tenant inválido.");
        if (query.FromInclusive > query.ToInclusive) throw new ArgumentException("Período inválido.");

        var snapshot = await _repo.GetOverviewAsync(
            tenantId: query.TenantId,
            fromInclusive: query.FromInclusive,
            toInclusive: query.ToInclusive,
            ct: ct
        );

        var avgTicket = snapshot.PaidOrdersCount <= 0 ? 0 : snapshot.RevenueCents / snapshot.PaidOrdersCount;

        var payments = snapshot.PaymentsByMethod
            .OrderBy(x => (int)x.Method)
            .Select(x => new DashboardPaymentMethodSummaryItem(x.Method, x.AmountCents, x.PaymentsCount))
            .ToList()
            .AsReadOnly();

        var providers = snapshot.PaymentsByProvider
            .OrderByDescending(x => x.AmountCents)
            .ThenBy(x => x.Provider)
            .Select(x => new DashboardPaymentProviderSummaryItem(x.Provider, x.AmountCents, x.PaymentsCount))
            .ToList()
            .AsReadOnly();

        var kitchen = snapshot.OrdersByKitchenStatus
            .OrderBy(x => (int)x.KitchenStatus)
            .Select(x => new DashboardKitchenStatusSummaryItem(x.KitchenStatus, x.OrdersCount))
            .ToList()
            .AsReadOnly();

        return new DashboardOverviewResult(
            FromInclusive: query.FromInclusive,
            ToInclusive: query.ToInclusive,
            OrdersCount: snapshot.OrdersCount,
            PaidOrdersCount: snapshot.PaidOrdersCount,
            CancelledOrdersCount: snapshot.CancelledOrdersCount,
            RevenueCents: snapshot.RevenueCents,
            AverageTicketCents: avgTicket,
            PaymentsByMethod: payments,
            PaymentsByProvider: providers,
            OrdersByKitchenStatus: kitchen
        );
    }
}

public sealed record GetDashboardOverviewQuery(Guid TenantId, DateTimeOffset FromInclusive, DateTimeOffset ToInclusive);

public sealed record DashboardOverviewResult(
    DateTimeOffset FromInclusive,
    DateTimeOffset ToInclusive,
    int OrdersCount,
    int PaidOrdersCount,
    int CancelledOrdersCount,
    int RevenueCents,
    int AverageTicketCents,
    IReadOnlyList<DashboardPaymentMethodSummaryItem> PaymentsByMethod,
    IReadOnlyList<DashboardPaymentProviderSummaryItem> PaymentsByProvider,
    IReadOnlyList<DashboardKitchenStatusSummaryItem> OrdersByKitchenStatus
);

public sealed record DashboardPaymentMethodSummaryItem(PaymentMethod Method, int AmountCents, int PaymentsCount);

public sealed record DashboardPaymentProviderSummaryItem(string Provider, int AmountCents, int PaymentsCount);

public sealed record DashboardKitchenStatusSummaryItem(OrderKitchenStatus KitchenStatus, int OrdersCount);
