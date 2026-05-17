using Microsoft.EntityFrameworkCore;

namespace SchoolsManagement.Api.Data;

public static class UserPagePermissionsBootstrap
{
    public static void EnsureTable(ApplicationDbContext db)
    {
        db.Database.ExecuteSqlRaw("""
            IF OBJECT_ID(N'dbo.user_page_permissions', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.user_page_permissions (
                    id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    user_id NVARCHAR(450) NOT NULL,
                    permission_key NVARCHAR(100) NOT NULL,
                    CONSTRAINT UQ_user_page_permissions_user_key UNIQUE (user_id, permission_key)
                );
                CREATE INDEX IX_user_page_permissions_user_id ON dbo.user_page_permissions(user_id);
            END
            """);
    }
}
