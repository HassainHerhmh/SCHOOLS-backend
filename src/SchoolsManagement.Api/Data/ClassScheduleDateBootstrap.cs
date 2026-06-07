using Microsoft.EntityFrameworkCore;

namespace SchoolsManagement.Api.Data;

/// <summary>إضافة عمود schedule_date للجداول الحالية (كل تاريخ له جدول مستقل).</summary>
public static class ClassScheduleDateBootstrap
{
    public static void EnsureColumns(ApplicationDbContext db)
    {
        db.Database.ExecuteSqlRaw("""
            IF COL_LENGTH(N'dbo.class_schedule_periods', N'schedule_date') IS NULL
            BEGIN
                ALTER TABLE dbo.class_schedule_periods ADD schedule_date date NULL;
            END
            """);

        db.Database.ExecuteSqlRaw("""
            UPDATE dbo.class_schedule_periods
            SET schedule_date = CAST(created_at AS date)
            WHERE schedule_date IS NULL;
            """);

        db.Database.ExecuteSqlRaw("""
            UPDATE dbo.class_schedule_periods
            SET schedule_date = CAST(GETDATE() AS date)
            WHERE schedule_date IS NULL;
            """);

        db.Database.ExecuteSqlRaw("""
            IF COL_LENGTH(N'dbo.class_schedule_periods', N'schedule_date') IS NOT NULL
               AND EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID(N'dbo.class_schedule_periods')
                      AND name = N'schedule_date'
                      AND is_nullable = 1)
            BEGIN
                ALTER TABLE dbo.class_schedule_periods ALTER COLUMN schedule_date date NOT NULL;
            END
            """);

        db.Database.ExecuteSqlRaw("""
            IF COL_LENGTH(N'dbo.class_schedule_custom_items', N'schedule_date') IS NULL
            BEGIN
                ALTER TABLE dbo.class_schedule_custom_items ADD schedule_date date NULL;
            END
            """);

        db.Database.ExecuteSqlRaw("""
            UPDATE dbo.class_schedule_custom_items
            SET schedule_date = CAST(created_at AS date)
            WHERE schedule_date IS NULL;
            """);

        db.Database.ExecuteSqlRaw("""
            UPDATE dbo.class_schedule_custom_items
            SET schedule_date = CAST(GETDATE() AS date)
            WHERE schedule_date IS NULL;
            """);

        db.Database.ExecuteSqlRaw("""
            IF COL_LENGTH(N'dbo.class_schedule_custom_items', N'schedule_date') IS NOT NULL
               AND EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID(N'dbo.class_schedule_custom_items')
                      AND name = N'schedule_date'
                      AND is_nullable = 1)
            BEGIN
                ALTER TABLE dbo.class_schedule_custom_items ALTER COLUMN schedule_date date NOT NULL;
            END
            """);
    }

    public static void EnsureIndexes(ApplicationDbContext db)
    {
        db.Database.ExecuteSqlRaw("""
            IF EXISTS (
                SELECT 1 FROM sys.key_constraints
                WHERE parent_object_id = OBJECT_ID(N'dbo.class_schedule_periods')
                  AND name = N'UQ_class_schedule_slot')
            BEGIN
                ALTER TABLE dbo.class_schedule_periods DROP CONSTRAINT UQ_class_schedule_slot;
            END
            """);

        db.Database.ExecuteSqlRaw("""
            IF NOT EXISTS (
                SELECT 1 FROM sys.key_constraints
                WHERE parent_object_id = OBJECT_ID(N'dbo.class_schedule_periods')
                  AND name = N'UQ_class_schedule_slot_date')
               AND NOT EXISTS (
                    SELECT 1
                    FROM dbo.class_schedule_periods
                    GROUP BY class_id, section_id, schedule_date, period_number
                    HAVING COUNT(*) > 1)
            BEGIN
                ALTER TABLE dbo.class_schedule_periods
                    ADD CONSTRAINT UQ_class_schedule_slot_date
                    UNIQUE (class_id, section_id, schedule_date, period_number);
            END
            """);

        db.Database.ExecuteSqlRaw("""
            IF NOT EXISTS (
                SELECT 1 FROM sys.indexes
                WHERE name = N'IX_class_schedule_periods_date'
                  AND object_id = OBJECT_ID(N'dbo.class_schedule_periods'))
            BEGIN
                CREATE INDEX IX_class_schedule_periods_date
                    ON dbo.class_schedule_periods(class_id, schedule_date);
            END
            """);

        db.Database.ExecuteSqlRaw("""
            IF NOT EXISTS (
                SELECT 1 FROM sys.indexes
                WHERE name = N'IX_class_schedule_custom_date'
                  AND object_id = OBJECT_ID(N'dbo.class_schedule_custom_items'))
            BEGIN
                CREATE INDEX IX_class_schedule_custom_date
                    ON dbo.class_schedule_custom_items(class_id, schedule_date);
            END
            """);
    }
}
