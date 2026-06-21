using Microsoft.EntityFrameworkCore;
using SchoolsManagement.Api.Configuration;

namespace SchoolsManagement.Api.Data;

public static class ParentsGradesTablesBootstrap
{
    private const string SqlServerSql = """
IF OBJECT_ID(N'dbo.parents_grades', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.parents_grades (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_parents_grades PRIMARY KEY,
        student_id uniqueidentifier NOT NULL,
        subject_id uniqueidentifier NOT NULL,
        subject_name nvarchar(250) NULL,
        exam_id uniqueidentifier NULL,
        exam_type nvarchar(80) NULL,
        exam_name nvarchar(250) NULL,
        Score decimal(18,2) NOT NULL,
        max_score decimal(18,2) NOT NULL,
        Percentage decimal(18,2) NULL,
        exam_date date NULL,
        academic_year int NOT NULL,
        Semester nvarchar(20) NOT NULL,
        Notes nvarchar(max) NULL,
        synced_at datetimeoffset(7) NOT NULL CONSTRAINT DF_parents_grades_synced DEFAULT (sysdatetimeoffset())
    );
    CREATE INDEX IX_parents_grades_student ON dbo.parents_grades(student_id, academic_year, Semester);
END

IF OBJECT_ID(N'dbo.parents_grade_rules', N'U') IS NULL
BEGIN
    /* reserved */
END

IF OBJECT_ID(N'dbo.parents_subjects', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.parents_subjects (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_parents_subjects PRIMARY KEY,
        name nvarchar(300) NOT NULL,
        class_id uniqueidentifier NULL,
        class_name nvarchar(200) NULL,
        max_score decimal(18,2) NOT NULL DEFAULT 100,
        synced_at datetimeoffset(7) NOT NULL CONSTRAINT DF_parents_subjects_synced DEFAULT (sysdatetimeoffset())
    );
END

IF OBJECT_ID(N'dbo.parents_exams', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.parents_exams (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_parents_exams PRIMARY KEY,
        subject_id uniqueidentifier NOT NULL,
        subject_name nvarchar(250) NULL,
        name nvarchar(250) NOT NULL,
        exam_type nvarchar(80) NOT NULL,
        max_score decimal(18,2) NOT NULL,
        exam_date date NULL,
        academic_year int NOT NULL,
        semester nvarchar(20) NOT NULL,
        month_key nvarchar(20) NULL,
        synced_at datetimeoffset(7) NOT NULL CONSTRAINT DF_parents_exams_synced DEFAULT (sysdatetimeoffset())
    );
    CREATE INDEX IX_parents_exams_subject ON dbo.parents_exams(subject_id, academic_year, semester);
END
""";

    private const string MySqlSql = """
CREATE TABLE IF NOT EXISTS parents_grades (
    Id char(36) NOT NULL PRIMARY KEY,
    student_id char(36) NOT NULL,
    subject_id char(36) NOT NULL,
    subject_name varchar(250) NULL,
    exam_id char(36) NULL,
    exam_type varchar(80) NULL,
    exam_name varchar(250) NULL,
    Score decimal(18,2) NOT NULL,
    max_score decimal(18,2) NOT NULL,
    Percentage decimal(18,2) NULL,
    exam_date date NULL,
    academic_year int NOT NULL,
    Semester varchar(20) NOT NULL,
    Notes text NULL,
    synced_at datetime(6) NOT NULL,
    INDEX IX_parents_grades_student (student_id, academic_year, Semester)
);

CREATE TABLE IF NOT EXISTS parents_subjects (
    Id char(36) NOT NULL PRIMARY KEY,
    name varchar(300) NOT NULL,
    class_id char(36) NULL,
    class_name varchar(200) NULL,
    max_score decimal(18,2) NOT NULL DEFAULT 100,
    synced_at datetime(6) NOT NULL
);

CREATE TABLE IF NOT EXISTS parents_exams (
    Id char(36) NOT NULL PRIMARY KEY,
    subject_id char(36) NOT NULL,
    subject_name varchar(250) NULL,
    name varchar(250) NOT NULL,
    exam_type varchar(80) NOT NULL,
    max_score decimal(18,2) NOT NULL,
    exam_date date NULL,
    academic_year int NOT NULL,
    semester varchar(20) NOT NULL,
    month_key varchar(20) NULL,
    synced_at datetime(6) NOT NULL,
    INDEX IX_parents_exams_subject (subject_id, academic_year, semester)
);
""";

    public static async Task EnsureExistsAsync(ApplicationDbContext db, CancellationToken cancellationToken = default)
    {
        if (DatabaseProviderHelper.IsMySql(db))
        {
            foreach (var statement in MySqlSql.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (statement.Length == 0) continue;
                await db.Database.ExecuteSqlRawAsync(statement, cancellationToken);
            }
            return;
        }

        await db.Database.ExecuteSqlRawAsync(SqlServerSql, cancellationToken);
    }
}
