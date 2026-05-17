using Microsoft.EntityFrameworkCore;

namespace SchoolsManagement.Api.Data;

/// <summary>
/// يضمن وجود عمود ربط الموظف بدليل الحسابات حتى لو فشلت أو لم تُسجَّل الهجرة في السجل.
/// </summary>
public static class EmployeePayrollSchemaBootstrap
{
    public static void EnsureEmployeeChartAccountColumn(ApplicationDbContext db)
    {
        db.Database.ExecuteSqlRaw(
            """
IF COL_LENGTH(N'dbo.employees', N'chart_account_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[employees] ADD [chart_account_id] int NULL;
END
""");

        db.Database.ExecuteSqlRaw(
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

        NormalizeEmployeeChartAccountColumnToInt(db);
    }

    /// <summary>
    /// إن وُجد العمود كنص (استيراد/يدوي) يُحوَّل إلى int حتى لا يفشل EF عند القراءة.
    /// </summary>
    public static void NormalizeEmployeeChartAccountColumnToInt(ApplicationDbContext db)
    {
        db.Database.ExecuteSqlRaw(
            """
IF COL_LENGTH(N'dbo.employees', N'chart_account_id') IS NOT NULL
BEGIN
    DECLARE @empChartType nvarchar(128);
    SELECT @empChartType = DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = N'dbo' AND TABLE_NAME = N'employees' AND COLUMN_NAME = N'chart_account_id';

    IF @empChartType IN (N'nvarchar', N'varchar', N'nchar', N'char')
    BEGIN
        IF EXISTS (
            SELECT 1 FROM sys.indexes
            WHERE name = N'IX_employees_chart_account_id_unique'
              AND object_id = OBJECT_ID(N'dbo.employees')
        )
            DROP INDEX [IX_employees_chart_account_id_unique] ON [dbo].[employees];

        ALTER TABLE [dbo].[employees] ADD [chart_account_id_tmp] int NULL;
        UPDATE [dbo].[employees] SET [chart_account_id_tmp] = TRY_CONVERT(int, [chart_account_id]);
        ALTER TABLE [dbo].[employees] DROP COLUMN [chart_account_id];
        EXEC sp_rename N'dbo.employees.chart_account_id_tmp', N'chart_account_id', N'COLUMN';

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
    END
END
""");
    }
}
