using Microsoft.EntityFrameworkCore;
using SchoolsManagement.Api.Configuration;
using SchoolsManagement.Api.Services;

namespace SchoolsManagement.Api.Data;

public static class DatabaseSchemaInitializer
{
    public static async Task ApplyAsync(
        ApplicationDbContext db,
        DatabaseConfigState config,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (!config.IsConfigured || ConnectionStringResolver.IsPlaceholderConnection(config.ConnectionString))
        {
            return;
        }

        if (config.IsMySql)
        {
            await ApplyMySqlRoyalAsync(db, logger, cancellationToken);
            return;
        }

        await ApplySqlServerAsync(db, logger, cancellationToken);
    }

    /// <summary>سيرفر رويال (MySQL): جداول parents_* فقط — بدون EnsureCreated لنظام المدارس كاملاً.</summary>
    private static async Task ApplyMySqlRoyalAsync(
        ApplicationDbContext db,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        try
        {
            await ParentsAppTablesBootstrap.EnsureMySqlParentsTablesAsync(db, cancellationToken);
            logger.LogInformation("MySQL (رويال): تم التأكد من جداول parents_* الأربعة فقط.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "MySQL: فشل إنشاء جداول parents_*.");
        }
    }

    private static async Task ApplySqlServerAsync(
        ApplicationDbContext db,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        try
        {
            await db.Database.MigrateAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "فشل تطبيق هجرات قاعدة البيانات.");
        }

        try { EmployeePayrollSchemaBootstrap.EnsureEmployeeChartAccountColumn(db); }
        catch (Exception ex) { logger.LogError(ex, "فشل chart_account_id للموظفين."); }

        try { AccountingVoucherTablesBootstrap.EnsureExists(db); }
        catch (Exception ex) { logger.LogError(ex, "فشل جداول السندات."); }

        try { JournalEntryPostedAtBootstrap.EnsurePostedAtColumn(db); }
        catch (Exception ex) { logger.LogError(ex, "فشل posted_at."); }

        try { UserPagePermissionsBootstrap.EnsureTable(db); }
        catch (Exception ex) { logger.LogError(ex, "فشل user_page_permissions."); }

        try { ApplicationUserPermissionsJsonBootstrap.EnsureColumn(db); }
        catch (Exception ex) { logger.LogError(ex, "فشل permissions_json."); }

        try { SchoolExtendedTablesBootstrap.EnsureExists(db); }
        catch (Exception ex) { logger.LogError(ex, "فشل الجداول الموسعة."); }

        try { ParentsAppTablesBootstrap.EnsureExists(db); }
        catch (Exception ex) { logger.LogError(ex, "فشل جداول parents_*."); }
    }
}
