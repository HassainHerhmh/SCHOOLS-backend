using Microsoft.EntityFrameworkCore;
using SchoolsManagement.Api.Configuration;
using SchoolsManagement.Api.Data;

#pragma warning disable EF1002

namespace SchoolsManagement.Api.Services;

public sealed class DatabaseHealthChecker
{
    private static readonly string[] KeyTables =
    [
        "AspNetUsers",
        "AspNetRoles",
        "students",
        "classes",
        "sections",
        "parents_student_summaries",
        "parents_class_publishes",
        "parents_section_publishes",
        "parents_attendance_summaries",
        "sync_checkpoints",
        "__EFMigrationsHistory"
    ];

    public async Task<DatabaseHealthReport> CheckAsync(
        ApplicationDbContext db,
        string connectionString,
        CancellationToken cancellationToken = default)
    {
        var isMySql = DatabaseProviderHelper.IsMySql(db);
        var report = new DatabaseHealthReport
        {
            Provider = db.Database.ProviderName ?? "unknown",
            ConnectionSummary = ConnectionStringResolver.RedactForDisplay(connectionString),
            EnvironmentHints = CollectEnvironmentHints()
        };

        if (isMySql && ConnectionStringResolver.HasMysqlEnvVars())
        {
            report.Warnings.Add("Railway MySQL — الجداول تُنشأ تلقائياً عند أول تشغيل (EnsureCreated).");
        }

        try
        {
            report.CanConnect = await db.Database.CanConnectAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            report.CanConnect = false;
            report.Error = ex.Message;
            report.Status = "error";
            return report;
        }

        if (!report.CanConnect)
        {
            report.Status = "error";
            report.Error = "CanConnectAsync returned false";
            return report;
        }

        try
        {
            List<TableRow> tables;
            if (isMySql)
            {
                report.DatabaseName = await db.Database.SqlQueryRaw<string>(
                        "SELECT DATABASE() AS `Value`")
                    .FirstOrDefaultAsync(cancellationToken) ?? "";
                report.ServerName = await db.Database.SqlQueryRaw<string>(
                        "SELECT @@hostname AS `Value`")
                    .FirstOrDefaultAsync(cancellationToken) ?? "";
                report.TableCount = await db.Database.SqlQueryRaw<int>(
                        "SELECT COUNT(*) AS `Value` FROM information_schema.tables WHERE table_schema = DATABASE()")
                    .FirstOrDefaultAsync(cancellationToken);
                tables = await db.Database.SqlQueryRaw<TableRow>(
                        """
                        SELECT table_schema AS `Schema`, table_name AS `Name`
                        FROM information_schema.tables
                        WHERE table_schema = DATABASE()
                        ORDER BY table_name
                        """)
                    .ToListAsync(cancellationToken);
            }
            else
            {
                report.DatabaseName = await db.Database.SqlQueryRaw<string>(
                        "SELECT DB_NAME() AS [Value]")
                    .FirstOrDefaultAsync(cancellationToken) ?? "";
                report.ServerName = await db.Database.SqlQueryRaw<string>(
                        "SELECT @@SERVERNAME AS [Value]")
                    .FirstOrDefaultAsync(cancellationToken) ?? "";
                report.TableCount = await db.Database.SqlQueryRaw<int>(
                        "SELECT COUNT(*) AS [Value] FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = N'BASE TABLE'")
                    .FirstOrDefaultAsync(cancellationToken);
                tables = await db.Database.SqlQueryRaw<TableRow>(
                        """
                        SELECT TABLE_SCHEMA AS [Schema], TABLE_NAME AS [Name]
                        FROM INFORMATION_SCHEMA.TABLES
                        WHERE TABLE_TYPE = N'BASE TABLE'
                        ORDER BY TABLE_NAME
                        """)
                    .ToListAsync(cancellationToken);
            }

            report.Tables = tables.Select(t => $"{t.Schema}.{t.Name}").ToList();

            foreach (var key in KeyTables)
            {
                var exists = tables.Any(t =>
                    string.Equals(t.Name, key, StringComparison.OrdinalIgnoreCase));
                int? count = null;
                if (exists)
                {
                    count = await TryCountTableAsync(db, key, cancellationToken);
                }

                report.KeyTables[key] = new KeyTableStatus { Exists = exists, RowCount = count };
            }

            if (!isMySql)
            {
                report.AppliedMigrations = (await db.Database.GetAppliedMigrationsAsync(cancellationToken)).ToList();
                report.PendingMigrations = (await db.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
                report.MigrationsAppliedCount = report.AppliedMigrations.Count;
                report.MigrationsPendingCount = report.PendingMigrations.Count;
            }
            else
            {
                report.Warnings.Add("MySQL: schema via EnsureCreated (لا EF migrations SQL Server).");
            }

            if (report.TableCount == 0)
            {
                report.Warnings.Add("لا توجد جداول — شغّل التطبيق مرة ليطبّق EF Migrate أو نفّذ Scripts/royal-ensure-all-tables.sql");
            }
            else if (report.MigrationsPendingCount > 0)
            {
                report.Warnings.Add($"هناك {report.MigrationsPendingCount} هجرة لم تُطبَّق بعد.");
            }

            if (report.KeyTables.TryGetValue("AspNetUsers", out var users) && users.Exists && users.RowCount == 0)
            {
                report.Warnings.Add("جدول AspNetUsers موجود لكن بدون مستخدمين — سجّل دخول أو انسخ قاعدة من .bak");
            }

            report.Status = report.Warnings.Count > 0 && report.TableCount == 0 ? "warning" : "ok";
        }
        catch (Exception ex)
        {
            report.Status = "error";
            report.Error = ex.Message;
        }

        return report;
    }

    private static List<string> CollectEnvironmentHints()
    {
        var keys = new[]
        {
            "MYSQL_PUBLIC_URL",
            "MYSQL_URL",
            "MYSQLHOST",
            "ConnectionStrings__DefaultConnection",
            "SQLSERVER_CONNECTION_STRING",
            "RAILWAY_ENVIRONMENT"
        };

        var hints = new List<string>();
        foreach (var key in keys)
        {
            var val = Environment.GetEnvironmentVariable(key);
            if (string.IsNullOrWhiteSpace(val))
            {
                continue;
            }

            hints.Add($"{key}={(val.Contains("Password", StringComparison.OrdinalIgnoreCase) || val.Contains("pwd=", StringComparison.OrdinalIgnoreCase) ? "(set, hidden)" : ConnectionStringResolver.RedactForDisplay(val))}");
        }

        return hints;
    }

    private static async Task<int?> TryCountTableAsync(
        ApplicationDbContext db,
        string tableName,
        CancellationToken cancellationToken)
    {
        if (!KeyTables.Contains(tableName, StringComparer.OrdinalIgnoreCase))
        {
            return null;
        }

        var sql = tableName switch
        {
            "AspNetUsers" => "SELECT COUNT(*) AS [Value] FROM [AspNetUsers]",
            "AspNetRoles" => "SELECT COUNT(*) AS [Value] FROM [AspNetRoles]",
            "students" => "SELECT COUNT(*) AS [Value] FROM [students]",
            "classes" => "SELECT COUNT(*) AS [Value] FROM [classes]",
            "sections" => "SELECT COUNT(*) AS [Value] FROM [sections]",
            "parents_student_summaries" => "SELECT COUNT(*) AS [Value] FROM [parents_student_summaries]",
            "parents_class_publishes" => "SELECT COUNT(*) AS [Value] FROM [parents_class_publishes]",
            "parents_section_publishes" => "SELECT COUNT(*) AS [Value] FROM [parents_section_publishes]",
            "parents_attendance_summaries" => "SELECT COUNT(*) AS [Value] FROM [parents_attendance_summaries]",
            "sync_checkpoints" => "SELECT COUNT(*) AS [Value] FROM [sync_checkpoints]",
            "__EFMigrationsHistory" => "SELECT COUNT(*) AS [Value] FROM [__EFMigrationsHistory]",
            _ => null
        };

        if (sql is null)
        {
            return null;
        }

        try
        {
            return await db.Database.SqlQueryRaw<int>(sql).FirstOrDefaultAsync(cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    private sealed class TableRow
    {
        public string Schema { get; set; } = "";
        public string Name { get; set; } = "";
    }
}

public sealed class DatabaseHealthReport
{
    public string Status { get; set; } = "ok";
    public string Provider { get; set; } = "";
    public string ConnectionSummary { get; set; } = "";
    public bool CanConnect { get; set; }
    public string? ServerName { get; set; }
    public string? DatabaseName { get; set; }
    public int TableCount { get; set; }
    public List<string> Tables { get; set; } = [];
    public Dictionary<string, KeyTableStatus> KeyTables { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public int MigrationsAppliedCount { get; set; }
    public int MigrationsPendingCount { get; set; }
    public List<string> AppliedMigrations { get; set; } = [];
    public List<string> PendingMigrations { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
    public List<string> EnvironmentHints { get; set; } = [];
    public string? Error { get; set; }
}

public sealed class KeyTableStatus
{
    public bool Exists { get; set; }
    public int? RowCount { get; set; }
}
