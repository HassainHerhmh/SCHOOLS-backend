namespace SchoolsManagement.Api.Security;

/// <summary>ربط مسارات الـ API بمفتاح الصلاحية المطلوب (عند وجود JWT).</summary>
public static class ApiPermissionMap
{
    private static readonly (string Prefix, string Permission)[] Rules =
    [
        ("/api/users", PermissionCatalog.SystemUsers),
        ("/api/permissions", PermissionCatalog.Permissions),
        ("/api/payroll", PermissionCatalog.AccountingEmployees),
        ("/api/students", PermissionCatalog.Students),
        ("/api/classes", PermissionCatalog.Classes),
        ("/api/sections", PermissionCatalog.Classes),
        ("/api/attendance", PermissionCatalog.StudentAttendance),
        ("/api/employees", PermissionCatalog.Employees),
        ("/api/bus-users", PermissionCatalog.BusUsers),
        ("/api/bus-sites", PermissionCatalog.BusSites),
        ("/api/payment-vouchers", PermissionCatalog.AccountingManagement),
        ("/api/receipt-vouchers", PermissionCatalog.AccountingManagement),
        ("/api/journal-entries", PermissionCatalog.AccountingManagement),
        ("/api/journal-posting", PermissionCatalog.AccountingManagement),
        ("/api/chart-accounts", PermissionCatalog.AccountingManagement),
        ("/api/account-groups", PermissionCatalog.AccountingManagement),
        ("/api/currencies", PermissionCatalog.AccountingManagement),
        ("/api/currency-exchanges", PermissionCatalog.AccountingManagement),
        ("/api/journal-types", PermissionCatalog.AccountingManagement),
        ("/api/payment-types", PermissionCatalog.AccountingManagement),
        ("/api/receipt-types", PermissionCatalog.AccountingManagement),
        ("/api/cash-boxes", PermissionCatalog.AccountingManagement),
        ("/api/cash-box-groups", PermissionCatalog.AccountingManagement),
        ("/api/banks", PermissionCatalog.AccountingManagement),
        ("/api/bank-groups", PermissionCatalog.AccountingManagement),
        ("/api/transit-accounts", PermissionCatalog.AccountingManagement),
        ("/api/sync", PermissionCatalog.SystemBackup),
    ];

    public static bool IsPublicApiPath(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return true;
        }

        return path.Equals("/api/health", StringComparison.OrdinalIgnoreCase)
               || path.StartsWith("/api/auth/login", StringComparison.OrdinalIgnoreCase)
               || path.StartsWith("/api/auth/register", StringComparison.OrdinalIgnoreCase)
               || path.StartsWith("/api/employees/login", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>مسارات تتطلب JWT دائماً حتى بدون خريطة صلاحية.</summary>
    public static bool RequiresAuthentication(string path) =>
        path.StartsWith("/api/users", StringComparison.OrdinalIgnoreCase)
        || path.StartsWith("/api/permissions", StringComparison.OrdinalIgnoreCase);

    public static string? GetRequiredPermission(string path)
    {
        foreach (var (prefix, permission) in Rules)
        {
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return permission;
            }
        }

        return null;
    }
}
