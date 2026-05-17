using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolsManagement.Api.Data;

#nullable disable

namespace SchoolsManagement.Api.Migrations;

/// <summary>
/// عمود chart_account_id على جدول الموظفين. يجب أن يكون مع [DbContext] حتى يكتشفها EF ويطبّق Migrate().
/// </summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20260515120000_EmployeeChartAccountLink")]
public class EmployeeChartAccountLink : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
IF COL_LENGTH(N'dbo.employees', N'chart_account_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[employees] ADD [chart_account_id] int NULL;
END
""");

        migrationBuilder.Sql(
            """
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_employees_chart_account_id_unique'
      AND object_id = OBJECT_ID(N'dbo.employees')
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX [IX_employees_chart_account_id_unique]
    ON [dbo].[employees] ([chart_account_id])
    WHERE [chart_account_id] IS NOT NULL;
END
""");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
IF EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_employees_chart_account_id_unique'
      AND object_id = OBJECT_ID(N'dbo.employees')
)
BEGIN
    DROP INDEX [IX_employees_chart_account_id_unique] ON [dbo].[employees];
END
""");

        migrationBuilder.Sql(
            """
IF COL_LENGTH(N'dbo.employees', N'chart_account_id') IS NOT NULL
BEGIN
    ALTER TABLE [dbo].[employees] DROP COLUMN [chart_account_id];
END
""");
    }
}
