using TotemAPI.Features.Kitchen.Application.Abstractions;
using TotemAPI.Features.Kitchen.Domain;

namespace TotemAPI.Features.Kitchen.Application.UseCases;

public sealed record UpsertKitchenSlaCommand(
    Guid TenantId,
    int QueuedTargetSeconds,
    int PreparationBaseTargetSeconds,
    int ReadyTargetSeconds
);

public sealed class UpsertKitchenSla
{
    public UpsertKitchenSla(IKitchenSlaRepository slas)
    {
        _slas = slas;
    }

    private readonly IKitchenSlaRepository _slas;

    public async Task<KitchenSlaResult> HandleAsync(UpsertKitchenSlaCommand command, CancellationToken ct)
    {
        if (command.TenantId == Guid.Empty) throw new ArgumentException("TenantId inválido.");
        if (command.QueuedTargetSeconds <= 0) throw new ArgumentException("QueuedTargetSeconds inválido.");
        if (command.PreparationBaseTargetSeconds <= 0) throw new ArgumentException("PreparationBaseTargetSeconds inválido.");
        if (command.ReadyTargetSeconds <= 0) throw new ArgumentException("ReadyTargetSeconds inválido.");

        var now = DateTimeOffset.UtcNow;
        var sla = new KitchenSla(
            TenantId: command.TenantId,
            QueuedTargetSeconds: command.QueuedTargetSeconds,
            PreparationBaseTargetSeconds: command.PreparationBaseTargetSeconds,
            ReadyTargetSeconds: command.ReadyTargetSeconds,
            UpdatedAt: now
        );

        await _slas.UpsertAsync(sla, ct);
        return new KitchenSlaResult(sla.QueuedTargetSeconds, sla.PreparationBaseTargetSeconds, sla.ReadyTargetSeconds, sla.UpdatedAt);
    }
}
