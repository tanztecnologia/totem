namespace TotemAPI.Features.Pos.Domain;

public enum CashRegisterShiftStatus
{
    Open = 1,
    Closed = 2
}

public sealed record CashRegisterShift(
    Guid Id,
    Guid TenantId,
    CashRegisterShiftStatus Status,
    Guid OpenedByUserId,
    string OpenedByEmail,
    int OpeningCashCents,
    DateTimeOffset OpenedAt,
    Guid? ClosedByUserId,
    string? ClosedByEmail,
    int? ClosingCashCents,
    int? TotalSalesCents,
    int? TotalCashSalesCents,
    int? ExpectedCashCents,
    DateTimeOffset? ClosedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);

