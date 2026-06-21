using Microsoft.EntityFrameworkCore;
using SchoolsManagement.Api.Configuration;
using SchoolsManagement.Api.Services;

namespace SchoolsManagement.Api.Data;

public static class BusPasswordMigrationBootstrap
{
    public static async Task MigratePlainPasswordsAsync(
        ApplicationDbContext db,
        string pepper,
        CancellationToken cancellationToken = default)
    {
        var changed = false;

        if (!DatabaseProviderHelper.IsMySql(db))
        {
            var portalUsers = await db.BusPortalUsers
                .Where(x => x.PasswordHash != null && x.PasswordHash != "")
                .ToListAsync(cancellationToken);
            foreach (var user in portalUsers)
            {
                if (EmployeePasswordHasher.IsStoredHash(user.PasswordHash))
                {
                    continue;
                }

                user.PasswordHash = EmployeePasswordHasher.Hash(user.PasswordHash, pepper);
                changed = true;
            }
        }

        if (await TableExistsAsync(db, "bus_app_drivers", cancellationToken))
        {
            var appDrivers = await db.BusAppDrivers
                .Where(x => x.PasswordHash != null && x.PasswordHash != "")
                .ToListAsync(cancellationToken);
            foreach (var driver in appDrivers)
            {
                if (EmployeePasswordHasher.IsStoredHash(driver.PasswordHash))
                {
                    continue;
                }

                driver.PasswordHash = EmployeePasswordHasher.Hash(driver.PasswordHash, pepper);
                changed = true;
            }
        }

        if (changed)
        {
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private static async Task<bool> TableExistsAsync(
        ApplicationDbContext db,
        string tableName,
        CancellationToken cancellationToken)
    {
        if (DatabaseProviderHelper.IsMySql(db))
        {
            var sql = "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = {0}";
            var count = await db.Database.SqlQueryRaw<int>(sql, tableName).FirstOrDefaultAsync(cancellationToken);
            return count > 0;
        }

        var sqlServerSql =
            "SELECT COUNT(*) AS [Value] FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = {0}";
        var sqlCount = await db.Database.SqlQueryRaw<int>(sqlServerSql, tableName).FirstOrDefaultAsync(cancellationToken);
        return sqlCount > 0;
    }
}
