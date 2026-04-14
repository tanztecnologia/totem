using TotemAPI.Features.Checkout.Application.Abstractions;
using TotemAPI.Features.Checkout.Domain;

namespace TotemAPI.Features.Pos.Application.UseCases;

public sealed class ListPosOrdersByComanda
{
    public ListPosOrdersByComanda(ICheckoutRepository repo)
    {
        _repo = repo;
    }

    private readonly ICheckoutRepository _repo;

    public async Task<IReadOnlyList<PosOrderListItem>> HandleAsync(ListPosOrdersByComandaQuery query, CancellationToken ct)
    {
        if (query.TenantId == Guid.Empty) throw new ArgumentException("Tenant inválido.");
        if (string.IsNullOrWhiteSpace(query.Comanda)) throw new ArgumentException("Comanda é obrigatória.");

        var list = await _repo.ListOrdersByComandaAsync(
            tenantId: query.TenantId,
            comanda: query.Comanda,
            includePaid: query.IncludePaid,
            limit: query.Limit,
            ct: ct
        );

        return list
            .Select(
                x =>
                    new PosOrderListItem(
                        OrderId: x.Id,
                        Comanda: x.Comanda,
                        Status: x.Status,
                        KitchenStatus: x.KitchenStatus,
                        TotalCents: x.TotalCents,
                        CreatedAt: x.CreatedAt,
                        UpdatedAt: x.UpdatedAt
                    )
            )
            .ToList()
            .AsReadOnly();
    }
}

public sealed record ListPosOrdersByComandaQuery(Guid TenantId, string Comanda, bool IncludePaid, int Limit);

public sealed record PosOrderListItem(
    Guid OrderId,
    string? Comanda,
    OrderStatus Status,
    OrderKitchenStatus KitchenStatus,
    int TotalCents,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);

