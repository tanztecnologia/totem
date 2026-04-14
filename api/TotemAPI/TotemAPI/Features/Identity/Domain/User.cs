namespace TotemAPI.Features.Identity.Domain;

public enum UserRole
{
    Admin = 1,
    Staff = 2,
    Totem = 3,
    Waiter = 4,
    Pdv = 5
}

public sealed record User(
    Guid Id,
    Guid TenantId,
    string Email,
    string PasswordHash,
    UserRole Role,
    DateTimeOffset CreatedAt
);
