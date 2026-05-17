using Microsoft.EntityFrameworkCore;

namespace SchoolsManagement.Api.Data;

public static class ApplicationUserPermissionsJsonBootstrap
{
    public static void EnsureColumn(ApplicationDbContext db)
    {
        db.Database.ExecuteSqlRaw("""
            IF COL_LENGTH('AspNetUsers', 'permissions_json') IS NULL
            BEGIN
                ALTER TABLE AspNetUsers ADD permissions_json NVARCHAR(MAX) NULL;
            END
            """);
    }
}
