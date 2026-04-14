using TotemAPI.Features.Checkout.Domain;
using TotemAPI.Features.Pos.Application.Abstractions;
using TotemAPI.Features.Pos.Domain;

namespace TotemAPI.Features.Pos.Application.UseCases;

public sealed record CloseCashRegisterShiftCommand(
    Guid TenantId,
    Guid UserId,
    string UserEmail,
    int ClosingCashCents
);

public sealed record CloseCashRegisterShiftResult(
    CashRegisterShiftResult Shift,
    int TotalSalesCents,
    int TotalCashSalesCents,
    int ExpectedCashCents,
    int ClosingCashCents,
    int DifferenceCents,
    IReadOnlyList<CloseCashRegisterPaymentSummaryItem> Payments
);

public sealed record CloseCashRegisterPaymentSummaryItem(PaymentMethod Method, int AmountCents);

public sealed class CloseCashRegisterShift
{
    public CloseCashRegisterShift(ICashRegisterRepository cashRegister)
    {
        _cashRegister = cashRegister;
    }

    private readonly ICashRegisterRepository _cashRegister;

    public async Task<CloseCashRegisterShiftResult> HandleAsync(CloseCashRegisterShiftCommand command, CancellationToken ct)
    {
        if (command.TenantId == Guid.Empty) throw new ArgumentException("Tenant inválido.");
        if (command.UserId == Guid.Empty) throw new ArgumentException("Usuário inválido.");
        if (string.IsNullOrWhiteSpace(command.UserEmail)) throw new ArgumentException("Email inválido.");
        if (command.ClosingCashCents < 0) throw new ArgumentException("Valor de fechamento inválido.");

        var shift = await _cashRegister.GetOpenShiftAsync(command.TenantId, ct);
        if (shift is null) throw new InvalidOperationException("Não existe caixa aberto.");

        var now = DateTimeOffset.UtcNow;
        var totalsByMethod = await _cashRegister.SumApprovedPosPaymentsByMethodAsync(
            tenantId: command.TenantId,
            fromInclusive: shift.OpenedAt,
            toInclusive: now,
            ct: ct
        );

        var totalSalesCents = totalsByMethod.Values.Sum();
        totalsByMethod.TryGetValue(PaymentMethod.Cash, out var cashSalesCents);
        var expectedCashCents = shift.OpeningCashCents + cashSalesCents;
        var differenceCents = command.ClosingCashCents - expectedCashCents;

        var closed = shift with
        {
            Status = CashRegisterShiftStatus.Closed,
            ClosedByUserId = command.UserId,
            ClosedByEmail = command.UserEmail.Trim(),
            ClosingCashCents = command.ClosingCashCents,
            TotalSalesCents = totalSalesCents,
            TotalCashSalesCents = cashSalesCents,
            ExpectedCashCents = expectedCashCents,
            ClosedAt = now,
            UpdatedAt = now
        };

        await _cashRegister.UpdateAsync(closed, ct);

        var paymentItems = totalsByMethod
            .OrderBy(x => (int)x.Key)
            .Select(x => new CloseCashRegisterPaymentSummaryItem(x.Key, x.Value))
            .ToList()
            .AsReadOnly();

        return new CloseCashRegisterShiftResult(
            Shift: new CashRegisterShiftResult(
                Id: closed.Id,
                Status: closed.Status,
                OpenedByEmail: closed.OpenedByEmail,
                OpeningCashCents: closed.OpeningCashCents,
                OpenedAt: closed.OpenedAt,
                ClosingCashCents: closed.ClosingCashCents,
                TotalSalesCents: closed.TotalSalesCents,
                TotalCashSalesCents: closed.TotalCashSalesCents,
                ExpectedCashCents: closed.ExpectedCashCents,
                ClosedAt: closed.ClosedAt
            ),
            TotalSalesCents: totalSalesCents,
            TotalCashSalesCents: cashSalesCents,
            ExpectedCashCents: expectedCashCents,
            ClosingCashCents: command.ClosingCashCents,
            DifferenceCents: differenceCents,
            Payments: paymentItems
        );
    }
}

