using TotemAPI.Features.Pos.Application.Abstractions;
using TotemAPI.Features.Pos.Domain;

namespace TotemAPI.Features.Pos.Application.UseCases;

public sealed record OpenCashRegisterShiftCommand(
    Guid TenantId,
    Guid UserId,
    string UserEmail,
    int OpeningCashCents
);

public sealed class OpenCashRegisterShift
{
    public OpenCashRegisterShift(ICashRegisterRepository cashRegister)
    {
        _cashRegister = cashRegister;
    }

    private readonly ICashRegisterRepository _cashRegister;

    public async Task<CashRegisterShiftResult> HandleAsync(OpenCashRegisterShiftCommand command, CancellationToken ct)
    {
        if (command.TenantId == Guid.Empty) throw new ArgumentException("Tenant inválido.");
        if (command.UserId == Guid.Empty) throw new ArgumentException("Usuário inválido.");
        if (string.IsNullOrWhiteSpace(command.UserEmail)) throw new ArgumentException("Email inválido.");
        if (command.OpeningCashCents < 0) throw new ArgumentException("Valor de abertura inválido.");

        var existing = await _cashRegister.GetOpenShiftAsync(command.TenantId, ct);
        if (existing is not null) throw new InvalidOperationException("Já existe um caixa aberto.");

        var now = DateTimeOffset.UtcNow;
        var shift = new CashRegisterShift(
            Id: Guid.NewGuid(),
            TenantId: command.TenantId,
            Status: CashRegisterShiftStatus.Open,
            OpenedByUserId: command.UserId,
            OpenedByEmail: command.UserEmail.Trim(),
            OpeningCashCents: command.OpeningCashCents,
            OpenedAt: now,
            ClosedByUserId: null,
            ClosedByEmail: null,
            ClosingCashCents: null,
            TotalSalesCents: null,
            TotalCashSalesCents: null,
            ExpectedCashCents: null,
            ClosedAt: null,
            CreatedAt: now,
            UpdatedAt: now
        );

        await _cashRegister.AddAsync(shift, ct);

        return new CashRegisterShiftResult(
            Id: shift.Id,
            Status: shift.Status,
            OpenedByEmail: shift.OpenedByEmail,
            OpeningCashCents: shift.OpeningCashCents,
            OpenedAt: shift.OpenedAt,
            ClosingCashCents: null,
            TotalSalesCents: null,
            TotalCashSalesCents: null,
            ExpectedCashCents: null,
            ClosedAt: null
        );
    }
}

