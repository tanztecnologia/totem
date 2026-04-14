using Microsoft.EntityFrameworkCore;
using TotemAPI.Features.Checkout.Domain;
using TotemAPI.Features.Pos.Application.Abstractions;
using TotemAPI.Features.Pos.Domain;

namespace TotemAPI.Infrastructure.Persistence;

public sealed class EfCashRegisterRepository : ICashRegisterRepository
{
    public EfCashRegisterRepository(TotemDbContext db)
    {
        _db = db;
    }

    private readonly TotemDbContext _db;

    public async Task<CashRegisterShift?> GetOpenShiftAsync(Guid tenantId, CancellationToken ct)
    {
        var list = await _db.CashRegisterShifts
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Status == CashRegisterShiftStatus.Open)
            .ToListAsync(ct);

        return list
            .OrderByDescending(x => x.OpenedAt)
            .FirstOrDefault()
            ?.ToDomain();
    }

    public async Task<CashRegisterShift?> GetByIdAsync(Guid tenantId, Guid shiftId, CancellationToken ct)
    {
        var row = await _db.CashRegisterShifts.AsNoTracking().SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Id == shiftId, ct);
        return row?.ToDomain();
    }

    public async Task AddAsync(CashRegisterShift shift, CancellationToken ct)
    {
        _db.CashRegisterShifts.Add(
            new CashRegisterShiftRow
            {
                Id = shift.Id,
                TenantId = shift.TenantId,
                Status = shift.Status,
                OpenedByUserId = shift.OpenedByUserId,
                OpenedByEmail = shift.OpenedByEmail,
                OpeningCashCents = shift.OpeningCashCents,
                OpenedAt = shift.OpenedAt,
                ClosedByUserId = shift.ClosedByUserId,
                ClosedByEmail = shift.ClosedByEmail,
                ClosingCashCents = shift.ClosingCashCents,
                TotalSalesCents = shift.TotalSalesCents,
                TotalCashSalesCents = shift.TotalCashSalesCents,
                ExpectedCashCents = shift.ExpectedCashCents,
                ClosedAt = shift.ClosedAt,
                CreatedAt = shift.CreatedAt,
                UpdatedAt = shift.UpdatedAt
            }
        );
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(CashRegisterShift shift, CancellationToken ct)
    {
        var row = await _db.CashRegisterShifts.SingleOrDefaultAsync(x => x.TenantId == shift.TenantId && x.Id == shift.Id, ct);
        if (row is null) throw new InvalidOperationException("Caixa não encontrado.");

        row.Status = shift.Status;
        row.ClosedByUserId = shift.ClosedByUserId;
        row.ClosedByEmail = shift.ClosedByEmail;
        row.ClosingCashCents = shift.ClosingCashCents;
        row.TotalSalesCents = shift.TotalSalesCents;
        row.TotalCashSalesCents = shift.TotalCashSalesCents;
        row.ExpectedCashCents = shift.ExpectedCashCents;
        row.ClosedAt = shift.ClosedAt;
        row.UpdatedAt = shift.UpdatedAt;

        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyDictionary<PaymentMethod, int>> SumApprovedPosPaymentsByMethodAsync(
        Guid tenantId,
        DateTimeOffset fromInclusive,
        DateTimeOffset toInclusive,
        CancellationToken ct
    )
    {
        var list = await _db.Payments
            .AsNoTracking()
            .Where(
                x =>
                    x.TenantId == tenantId
                    && x.Provider == "POS"
                    && x.Status == PaymentStatus.Approved
                    && x.UpdatedAt >= fromInclusive
                    && x.UpdatedAt <= toInclusive
            )
            .GroupBy(x => x.Method)
            .Select(g => new { Method = g.Key, Total = g.Sum(x => x.AmountCents) })
            .ToListAsync(ct);

        return list.ToDictionary(x => x.Method, x => x.Total);
    }
}
