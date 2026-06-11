using Microsoft.EntityFrameworkCore;
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

        if (changed)
        {
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
