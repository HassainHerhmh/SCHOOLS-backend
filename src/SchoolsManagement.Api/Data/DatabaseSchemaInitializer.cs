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
            await ApplyMySqlAsync(db, logger, cancellationToken);
            return;
        }

        await ApplySqlServerAsync(db, logger, cancellationToken);
    }

    private static async Task ApplyMySqlAsync(
        ApplicationDbContext db,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        try
        {
            var tableCount = await db.Database.SqlQueryRaw<int>(
                    "SELECT COUNT(*) AS Value FROM information_schema.tables WHERE table_schema = DATABASE()")
                .FirstOrDefaultAsync(cancellationToken);

            if (tableCount == 0)
            {
                var created = await db.Database.EnsureCreatedAsync(cancellationToken);
                logger.LogInformation("MySQL: EnsureCreated = {Created}", created);
            }
            else
            {
                logger.LogInformation("MySQL: {Count} tables already exist — skip EnsureCreated", tableCount);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "MySQL: فشل إنشاء الجداول.");
        }

        try
        {
            await EnsureMySqlSyncCheckpointsAsync(db, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "MySQL: فشل sync_checkpoints.");
        }
    }

    private static async Task EnsureMySqlSyncCheckpointsAsync(ApplicationDbContext db, CancellationToken cancellationToken)
    {
        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS sync_checkpoints (
                `Key` varchar(120) NOT NULL,
                synced_at datetime(6) NOT NULL,
                PRIMARY KEY (`Key`)
            );
            """,
            cancellationToken);
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
