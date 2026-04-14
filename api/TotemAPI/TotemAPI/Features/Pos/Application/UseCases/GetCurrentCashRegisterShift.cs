using TotemAPI.Features.Pos.Application.Abstractions;
using TotemAPI.Features.Pos.Domain;

namespace TotemAPI.Features.Pos.Application.UseCases;

public sealed record CashRegisterShiftResult(
    Guid Id,
    CashRegisterShiftStatus Status,
    string OpenedByEmail,
    int OpeningCashCents,
    DateTimeOffset OpenedAt,
    int? ClosingCashCents,
    int? TotalSalesCents,
    int? TotalCashSalesCents,
    int? ExpectedCashCents,
    DateTimeOffset? ClosedAt
);

public sealed class GetCurrentCashRegisterShift
{
    public GetCurrentCashRegisterShift(ICashRegisterRepository cashRegister)
    {
        _cashRegister = cashRegister;
    }

    private readonly ICashRegisterRepository _cashRegister;

    public async Task<CashRegisterShiftResult?> HandleAsync(Guid tenantId, CancellationToken ct)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant inválido.");

        var shift = await _cashRegister.GetOpenShiftAsync(tenantId, ct);
        if (shift is null) return null;

        return new CashRegisterShiftResult(
            Id: shift.Id,
            Status: shift.Status,
            OpenedByEmail: shift.OpenedByEmail,
            OpeningCashCents: shift.OpeningCashCents,
            OpenedAt: shift.OpenedAt,
            ClosingCashCents: shift.ClosingCashCents,
            TotalSalesCents: shift.TotalSalesCents,
            TotalCashSalesCents: shift.TotalCashSalesCents,
            ExpectedCashCents: shift.ExpectedCashCents,
            ClosedAt: shift.ClosedAt
        );
    }
}

