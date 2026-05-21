using Microsoft.EntityFrameworkCore;
using SchoolsManagement.Api.Configuration;

namespace SchoolsManagement.Api.Data;

/// <summary>جداول نشر بيانات تطبيق أولياء الأمور فقط (بدون جداول النظام المحاسبي).</summary>
public static class ParentsAppTablesBootstrap
{
    private const string SqlServerSql = """
IF OBJECT_ID(N'dbo.parents_students_summary', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.parents_students_summary (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_parents_students_summary PRIMARY KEY,
        parent_phone nvarchar(40) NULL,
        email nvarchar(250) NULL,
        name nvarchar(500) NOT NULL,
        level nvarchar(200) NOT NULL,
        section nvarchar(200) NOT NULL,
        paid_amount decimal(18,2) NOT NULL CONSTRAINT DF_parents_students_paid DEFAULT ((0)),
        school_fees decimal(18,2) NOT NULL CONSTRAINT DF_parents_students_school DEFAULT ((0)),
        uniform_fees decimal(18,2) NOT NULL CONSTRAINT DF_parents_students_uniform DEFAULT ((0)),
        bus_fees decimal(18,2) NOT NULL CONSTRAINT DF_parents_students_bus DEFAULT ((0)),
        synced_at datetimeoffset(7) NOT NULL CONSTRAINT DF_parents_students_synced DEFAULT (sysdatetimeoffset())
    );
    CREATE INDEX IX_parents_students_parent_phone ON dbo.parents_students_summary(parent_phone);
END

IF OBJECT_ID(N'dbo.parents_classes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.parents_classes (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_parents_classes PRIMARY KEY,
        name nvarchar(300) NOT NULL,
        level nvarchar(100) NOT NULL,
        display_order int NOT NULL CONSTRAINT DF_parents_classes_order DEFAULT ((0)),
        tuition_fees decimal(18,2) NOT NULL CONSTRAINT DF_parents_classes_tuition DEFAULT ((0)),
        uniform_fees decimal(18,2) NOT NULL CONSTRAINT DF_parents_classes_uniform DEFAULT ((0)),
        bus_fees decimal(18,2) NOT NULL CONSTRAINT DF_parents_classes_bus DEFAULT ((0)),
        synced_at datetimeoffset(7) NOT NULL CONSTRAINT DF_parents_classes_synced DEFAULT (sysdatetimeoffset())
    );
END

IF OBJECT_ID(N'dbo.parents_sections', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.parents_sections (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_parents_sections PRIMARY KEY,
        name nvarchar(300) NOT NULL,
        class_id uniqueidentifier NOT NULL,
        teacher_id uniqueidentifier NULL,
        teacher_name nvarchar(300) NULL,
        synced_at datetimeoffset(7) NOT NULL CONSTRAINT DF_parents_sections_synced DEFAULT (sysdatetimeoffset())
    );
    CREATE INDEX IX_parents_sections_class ON dbo.parents_sections(class_id);
END

IF OBJECT_ID(N'dbo.parents_attendance_summary', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.parents_attendance_summary (
        student_id uniqueidentifier NOT NULL,
        date date NOT NULL,
        status nvarchar(50) NOT NULL,
        synced_at datetimeoffset(7) NOT NULL CONSTRAINT DF_parents_attendance_synced DEFAULT (sysdatetimeoffset()),
        CONSTRAINT PK_parents_attendance_summary PRIMARY KEY (student_id, date)
    );
END
""";

    /// <summary>أعمدة متوافقة مع EF/Pomelo — لا تُنشئ جداول AspNetUsers أو المحاسبة.</summary>
    private const string MySqlSql = """
CREATE TABLE IF NOT EXISTS parents_students_summary (
    Id char(36) NOT NULL,
    parent_phone varchar(40) NULL,
    Email varchar(250) NULL,
    Name varchar(500) NOT NULL,
    Level varchar(200) NOT NULL,
    Section varchar(200) NOT NULL,
    paid_amount decimal(18,2) NOT NULL DEFAULT 0,
    school_fees decimal(18,2) NOT NULL DEFAULT 0,
    uniform_fees decimal(18,2) NOT NULL DEFAULT 0,
    bus_fees decimal(18,2) NOT NULL DEFAULT 0,
    synced_at datetime(6) NOT NULL,
    PRIMARY KEY (Id),
    INDEX IX_parents_students_parent_phone (parent_phone)
);

CREATE TABLE IF NOT EXISTS parents_classes (
    Id char(36) NOT NULL,
    Name varchar(300) NOT NULL,
    Level varchar(100) NOT NULL,
    display_order int NOT NULL DEFAULT 0,
    tuition_fees decimal(18,2) NOT NULL DEFAULT 0,
    uniform_fees decimal(18,2) NOT NULL DEFAULT 0,
    bus_fees decimal(18,2) NOT NULL DEFAULT 0,
    synced_at datetime(6) NOT NULL,
    PRIMARY KEY (Id)
);

CREATE TABLE IF NOT EXISTS parents_sections (
    Id char(36) NOT NULL,
    Name varchar(300) NOT NULL,
    class_id char(36) NOT NULL,
    teacher_id char(36) NULL,
    teacher_name varchar(300) NULL,
    synced_at datetime(6) NOT NULL,
    PRIMARY KEY (Id),
    INDEX IX_parents_sections_class (class_id)
);

CREATE TABLE IF NOT EXISTS parents_attendance_summary (
    student_id char(36) NOT NULL,
    date date NOT NULL,
    Status varchar(50) NOT NULL,
    synced_at datetime(6) NOT NULL,
    PRIMARY KEY (student_id, date)
);
""";

    public static void EnsureExists(ApplicationDbContext db)
    {
        if (DatabaseProviderHelper.IsMySql(db))
        {
            db.Database.ExecuteSqlRaw(MySqlSql);
            return;
        }

        db.Database.ExecuteSqlRaw(SqlServerSql);
    }

    public static async Task EnsureExistsAsync(ApplicationDbContext db, CancellationToken cancellationToken = default)
    {
        if (DatabaseProviderHelper.IsMySql(db))
        {
            await EnsureMySqlParentsTablesAsync(db, cancellationToken);
            return;
        }

        EnsureExists(db);
    }

    public static async Task EnsureMySqlParentsTablesAsync(
        ApplicationDbContext db,
        CancellationToken cancellationToken = default)
    {
        foreach (var statement in MySqlSql.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (statement.Length == 0)
            {
                continue;
            }

            await db.Database.ExecuteSqlRawAsync(statement, cancellationToken);
        }
    }
}
