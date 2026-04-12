using TotemAPI.Features.Kitchen.Application.Abstractions;

namespace TotemAPI.Features.Kitchen.Application.UseCases;

public sealed record GetKitchenSlaQuery(Guid TenantId);

public sealed record KitchenSlaResult(
    int QueuedTargetSeconds,
    int PreparationBaseTargetSeconds,
    int ReadyTargetSeconds,
    DateTimeOffset? UpdatedAt
);

public sealed class GetKitchenSla
{
    public GetKitchenSla(IKitchenSlaRepository slas)
    {
        _slas = slas;
    }

    private readonly IKitchenSlaRepository _slas;

    public static KitchenSlaResult Defaults()
    {
        return new KitchenSlaResult(
            QueuedTargetSeconds: 120,
            PreparationBaseTargetSeconds: 480,
            ReadyTargetSeconds: 120,
            UpdatedAt: null
        );
    }

    public async Task<KitchenSlaResult> HandleAsync(GetKitchenSlaQuery query, CancellationToken ct)
    {
        if (query.TenantId == Guid.Empty) throw new ArgumentException("TenantId inválido.");

        var sla = await _slas.GetAsync(query.TenantId, ct);
        if (sla is null) return Defaults();

        return new KitchenSlaResult(
            QueuedTargetSeconds: sla.QueuedTargetSeconds,
            PreparationBaseTargetSeconds: sla.PreparationBaseTargetSeconds,
            ReadyTargetSeconds: sla.ReadyTargetSeconds,
            UpdatedAt: sla.UpdatedAt
        );
    }
}
