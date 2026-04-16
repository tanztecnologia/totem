using System.Security.Claims;

namespace TotemAPI.Infrastructure.Auth;

public static class ClaimsPrincipalExtensions
{
    public const string PermissionClaimType = "perm";

    public static bool HasPermission(this ClaimsPrincipal user, string permission)
    {
        if (user.Identity?.IsAuthenticated != true) return false;
        return user.HasClaim(PermissionClaimType, permission);
    }
}

