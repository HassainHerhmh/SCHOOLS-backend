namespace SchoolsManagement.Api.Configuration;

/// <summary>ربط SQL محلي أو سحابي (Railway / Azure SQL).</summary>
public static class ConnectionStringResolver
{
    public static string Resolve(IConfiguration configuration)
    {
        var candidates = new[]
        {
            configuration.GetConnectionString("DefaultConnection"),
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection"),
            Environment.GetEnvironmentVariable("SQLSERVER_CONNECTION_STRING"),
            Environment.GetEnvironmentVariable("CUSTOMCONNSTR_DefaultConnection"),
        };

        foreach (var cs in candidates)
        {
            if (string.IsNullOrWhiteSpace(cs))
            {
                continue;
            }

            var trimmed = cs.Trim();
            if (trimmed.Length > 0)
            {
                return trimmed;
            }
        }

        throw new InvalidOperationException(
            "Connection string مفقود. عيّن ConnectionStrings:DefaultConnection أو متغير ConnectionStrings__DefaultConnection.");
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

    public static string RedactForDisplay(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return "(empty)";
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
