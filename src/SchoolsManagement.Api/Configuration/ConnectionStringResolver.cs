namespace SchoolsManagement.Api.Configuration;

/// <summary>ربط SQL محلي أو سحابي (Railway / Azure SQL).</summary>
public static class ConnectionStringResolver
{
    public static string? TryResolve(IConfiguration configuration)
    {
        foreach (var cs in EnumerateCandidates(configuration))
        {
            if (!string.IsNullOrWhiteSpace(cs))
            {
                return cs.Trim();
            }
        }

        return null;
    }

    public static string Resolve(IConfiguration configuration)
    {
        return TryResolve(configuration)
               ?? throw new InvalidOperationException(BuildMissingConnectionMessage());
    }

    public static IEnumerable<string?> EnumerateCandidates(IConfiguration configuration)
    {
        yield return configuration.GetConnectionString("DefaultConnection");
        yield return Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        yield return Environment.GetEnvironmentVariable("SQLSERVER_CONNECTION_STRING");
        yield return Environment.GetEnvironmentVariable("CUSTOMCONNSTR_DefaultConnection");
        yield return Environment.GetEnvironmentVariable("DATABASE_URL");
        yield return BuildFromSqlServerParts();
    }

    private static string? BuildFromSqlServerParts()
    {
        var host = FirstEnv("SQLSERVER_HOST", "MSSQL_HOST", "SQLSERVER_PRIVATE_HOST");
        var port = FirstEnv("SQLSERVER_PORT", "MSSQL_PORT") ?? "1433";
        var user = FirstEnv("SQLSERVER_USER", "MSSQL_USER", "SQLSERVER_USERNAME");
        var password = FirstEnv("SQLSERVER_PASSWORD", "MSSQL_PASSWORD", "SQLSERVER_PASS");
        var database = FirstEnv("SQLSERVER_DATABASE", "MSSQL_DATABASE", "SQLSERVER_DB") ?? "SchoolsDb";

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(password))
        {
            return null;
        }

        return
            $"Server={host},{port};Database={database};User Id={user};Password={password};Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=true";
    }

    private static string? FirstEnv(params string[] keys)
    {
        foreach (var key in keys)
        {
            var v = Environment.GetEnvironmentVariable(key);
            if (!string.IsNullOrWhiteSpace(v))
            {
                return v.Trim();
            }
        }

        return null;
    }

    public static string BuildMissingConnectionMessage()
    {
        var hints = new List<string>
        {
            "Connection string مفقود على السيرفر.",
            "",
            "أضف في Railway (خدمة SCHOOLS-backend) Variables:",
            "  ConnectionStrings__DefaultConnection",
            "  = Server=xxx.database.windows.net,1433;Database=SchoolsDb;User Id=...;Password=...;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=true",
            "",
            "ملاحظة: المشروع يستخدم SQL Server فقط (Entity Framework SqlServer) — وليس MySQL."
        };

        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("MYSQL_PUBLIC_URL"))
            || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("MYSQL_URL")))
        {
            hints.Add("");
            hints.Add("وُجد MYSQL_PUBLIC_URL على Railway — لا يصلح لهذا API.");
            hints.Add("أنشئ Azure SQL (أو SQL Server) واربط ConnectionStrings__DefaultConnection به.");
        }

        if (IsRailwayHost())
        {
            hints.Add("");
            hints.Add("بعد الحفظ: Redeploy ثم افتح GET /api/health/db");
        }

        return string.Join(Environment.NewLine, hints);
    }

    public static bool LooksLikeLocalSql(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return true;
        }

        return connectionString.Contains("localhost", StringComparison.OrdinalIgnoreCase)
               || connectionString.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase)
               || connectionString.Contains(@"localhost\SQLEXPRESS", StringComparison.OrdinalIgnoreCase)
               || connectionString.Contains("SQLEXPRESS", StringComparison.OrdinalIgnoreCase);
    }

    public static bool LooksLikeMySql(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return false;
        }

        return connectionString.Contains("mysql", StringComparison.OrdinalIgnoreCase)
               || connectionString.StartsWith("mysql://", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsPlaceholderConnection(string? connectionString)
    {
        return string.Equals(connectionString, PlaceholderConnectionString, StringComparison.Ordinal);
    }

    public const string PlaceholderConnectionString =
        "Server=railway-configure-sql.invalid,1433;Database=SchoolsDb;User Id=setup;Password=setup;Encrypt=True;TrustServerCertificate=True;Connect Timeout=2;MultipleActiveResultSets=true";

    public static string RedactForDisplay(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return "(empty)";
        }

        if (IsPlaceholderConnection(connectionString))
        {
            return "(not configured — placeholder)";
        }

        var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);
        var safe = parts
            .Where(p =>
                !p.TrimStart().StartsWith("Password", StringComparison.OrdinalIgnoreCase)
                && !p.TrimStart().StartsWith("Pwd", StringComparison.OrdinalIgnoreCase))
            .Select(p => p.Trim());
        return string.Join("; ", safe);
    }

    public static bool IsRailwayHost()
    {
        return !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("RAILWAY_ENVIRONMENT"))
               || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("RAILWAY_PROJECT_ID"))
               || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("PORT"));
    }
}
