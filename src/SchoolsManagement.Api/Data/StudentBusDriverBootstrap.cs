using Microsoft.EntityFrameworkCore;

namespace SchoolsManagement.Api.Data;

public static class StudentBusDriverBootstrap
{
    private const string Sql = """
IF COL_LENGTH(N'dbo.students', N'bus_driver_id') IS NULL
BEGIN
    ALTER TABLE dbo.students ADD bus_driver_id uniqueidentifier NULL;
END

IF COL_LENGTH(N'dbo.students', N'bus_driver_name') IS NULL
BEGIN
    ALTER TABLE dbo.students ADD bus_driver_name nvarchar(500) NULL;
END

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_students_bus_driver_id'
      AND object_id = OBJECT_ID(N'dbo.students'))
BEGIN
    CREATE INDEX IX_students_bus_driver_id ON dbo.students(bus_driver_id);
END
""";

    public static void EnsureColumns(ApplicationDbContext db) =>
        db.Database.ExecuteSqlRaw(Sql);
}
