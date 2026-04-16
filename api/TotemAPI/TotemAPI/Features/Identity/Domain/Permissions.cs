namespace TotemAPI.Features.Identity.Domain;

public static class Permissions
{
    public const string UsersManage = "users:manage";

    public const string CatalogRead = "catalog:read";
    public const string CatalogWrite = "catalog:write";

    public const string CartRead = "cart:read";
    public const string CartWrite = "cart:write";

    public const string CheckoutRead = "checkout:read";
    public const string CheckoutWrite = "checkout:write";

    public const string DashboardRead = "dashboard:read";

    public const string KitchenRead = "kitchen:read";
    public const string KitchenWrite = "kitchen:write";
    public const string KitchenSlaManage = "kitchen_sla:manage";

    public const string PosRead = "pos:read";
    public const string PosWrite = "pos:write";

    public static IReadOnlyList<string> ForRole(UserRole role)
    {
        return role switch
        {
            UserRole.Admin => new[]
            {
                UsersManage,
                CatalogRead,
                CatalogWrite,
                CartRead,
                CartWrite,
                CheckoutRead,
                CheckoutWrite,
                DashboardRead,
                KitchenRead,
                KitchenWrite,
                KitchenSlaManage,
                PosRead,
                PosWrite
            },
            UserRole.Staff => new[]
            {
                CatalogRead,
                CatalogWrite,
                CartRead,
                CartWrite,
                CheckoutRead,
                CheckoutWrite,
                KitchenRead,
                KitchenWrite,
                PosRead,
                PosWrite
            },
            UserRole.Totem => new[]
            {
                CatalogRead,
                CartRead,
                CartWrite,
                CheckoutWrite
            },
            UserRole.Waiter => new[]
            {
                CatalogRead,
                CartRead,
                CartWrite,
                CheckoutWrite
            },
            UserRole.Pdv => new[]
            {
                PosRead,
                PosWrite
            },
            _ => Array.Empty<string>()
        };
    }
}

