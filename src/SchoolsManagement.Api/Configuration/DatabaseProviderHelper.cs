using Microsoft.EntityFrameworkCore;

namespace SchoolsManagement.Api.Configuration;

public static class DatabaseProviderHelper
{
    public static bool IsMySql(DbContext db) =>
        db.Database.ProviderName?.Contains("MySql", StringComparison.OrdinalIgnoreCase) == true;

    public static bool IsMySqlConnectionString(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return false;
        }

        if (ConnectionStringResolver.LooksLikeMySql(connectionString))
        {
            return true;
        }

        return connectionString.Contains("Port=", StringComparison.OrdinalIgnoreCase)
               && !ConnectionStringResolver.LooksLikeLocalSql(connectionString);
    }
}
