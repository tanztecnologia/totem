using TotemAPI.Features.Checkout.Domain;
using TotemAPI.Features.Pos.Domain;

namespace TotemAPI.Features.Pos.Application.Abstractions;

public interface ICashRegisterRepository
{
    Task<CashRegisterShift?> GetOpenShiftAsync(Guid tenantId, CancellationToken ct);
    Task<CashRegisterShift?> GetByIdAsync(Guid tenantId, Guid shiftId, CancellationToken ct);
    Task AddAsync(CashRegisterShift shift, CancellationToken ct);
    Task UpdateAsync(CashRegisterShift shift, CancellationToken ct);
    Task<IReadOnlyDictionary<PaymentMethod, int>> SumApprovedPosPaymentsByMethodAsync(
        Guid tenantId,
        DateTimeOffset fromInclusive,
        DateTimeOffset toInclusive,
        CancellationToken ct
    );
}

