using Microsoft.EntityFrameworkCore;

namespace SchoolsManagement.Api.Data;

/// <summary>جداول الطلاب الإضافية (مدفوعات، خصومات، درجات، مواد، امتحانات، اعتماد حوالات).</summary>
public static class SchoolExtendedTablesBootstrap
{
    private const string Sql = """
IF OBJECT_ID(N'dbo.student_payments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.student_payments (
        id uniqueidentifier NOT NULL CONSTRAINT PK_student_payments PRIMARY KEY,
        student_id uniqueidentifier NOT NULL,
        student_name nvarchar(300) NULL,
        amount decimal(18,2) NOT NULL CONSTRAINT DF_student_payments_amount DEFAULT ((0)),
        payment_date date NOT NULL,
        receipt_number nvarchar(80) NOT NULL,
        school_fees_paid decimal(18,2) NOT NULL CONSTRAINT DF_student_payments_school DEFAULT ((0)),
        uniform_fees_paid decimal(18,2) NOT NULL CONSTRAINT DF_student_payments_uniform DEFAULT ((0)),
        bus_fees_paid decimal(18,2) NOT NULL CONSTRAINT DF_student_payments_bus DEFAULT ((0)),
        books_fees_paid decimal(18,2) NOT NULL CONSTRAINT DF_student_payments_books DEFAULT ((0)),
        payment_type nvarchar(80) NULL,
        notes nvarchar(max) NULL,
        created_at datetimeoffset(7) NOT NULL CONSTRAINT DF_student_payments_created DEFAULT (sysdatetimeoffset())
    );
    CREATE INDEX IX_student_payments_student ON dbo.student_payments(student_id);
    CREATE INDEX IX_student_payments_date ON dbo.student_payments(payment_date);
END

IF OBJECT_ID(N'dbo.transfer_approval_requests', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.transfer_approval_requests (
        id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_transfer_approval_requests PRIMARY KEY,
        parent_name nvarchar(300) NOT NULL,
        student_id uniqueidentifier NULL,
        student_name nvarchar(300) NULL,
        amount decimal(18,2) NOT NULL,
        payment_method nvarchar(80) NOT NULL,
        transfer_no nvarchar(120) NOT NULL,
        bank_id int NULL,
        notes nvarchar(max) NULL,
        status nvarchar(40) NOT NULL CONSTRAINT DF_transfer_status DEFAULT (N'pending'),
        currency_id int NULL,
        created_at datetimeoffset(7) NOT NULL CONSTRAINT DF_transfer_created DEFAULT (sysdatetimeoffset()),
        approved_at datetimeoffset(7) NULL,
        approved_by nvarchar(200) NULL
    );
END

IF OBJECT_ID(N'dbo.student_discounts', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.student_discounts (
        id uniqueidentifier NOT NULL CONSTRAINT PK_student_discounts PRIMARY KEY,
        name nvarchar(200) NOT NULL,
        amount decimal(18,2) NOT NULL,
        description nvarchar(max) NULL,
        is_active bit NOT NULL CONSTRAINT DF_student_discounts_active DEFAULT ((1)),
        created_at datetimeoffset(7) NOT NULL,
        updated_at datetimeoffset(7) NULL
    );
END

IF OBJECT_ID(N'dbo.student_discount_applications', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.student_discount_applications (
        id uniqueidentifier NOT NULL CONSTRAINT PK_student_discount_applications PRIMARY KEY,
        student_id uniqueidentifier NOT NULL,
        discount_id uniqueidentifier NOT NULL,
        discount_name nvarchar(200) NOT NULL,
        amount decimal(18,2) NOT NULL,
        applied_at datetimeoffset(7) NOT NULL,
        notes nvarchar(max) NULL,
        created_by nvarchar(200) NULL
    );
    CREATE INDEX IX_discount_apps_student ON dbo.student_discount_applications(student_id);
    CREATE INDEX IX_discount_apps_discount ON dbo.student_discount_applications(discount_id);
END

IF OBJECT_ID(N'dbo.subjects', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.subjects (
        id uniqueidentifier NOT NULL CONSTRAINT PK_subjects PRIMARY KEY,
        class_id uniqueidentifier NOT NULL,
        name nvarchar(250) NOT NULL,
        teacher_id uniqueidentifier NULL,
        teacher_name nvarchar(250) NULL,
        created_at datetimeoffset(7) NULL,
        updated_at datetimeoffset(7) NULL
    );
    CREATE INDEX IX_subjects_class ON dbo.subjects(class_id);
END

IF OBJECT_ID(N'dbo.exams', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.exams (
        id uniqueidentifier NOT NULL CONSTRAINT PK_exams PRIMARY KEY,
        subject_id uniqueidentifier NOT NULL,
        title nvarchar(250) NOT NULL,
        exam_date date NULL,
        max_score decimal(18,2) NOT NULL CONSTRAINT DF_exams_max DEFAULT ((100)),
        created_at datetimeoffset(7) NULL,
        updated_at datetimeoffset(7) NULL
    );
    CREATE INDEX IX_exams_subject ON dbo.exams(subject_id);
END

IF OBJECT_ID(N'dbo.grade_rules', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.grade_rules (
        id uniqueidentifier NOT NULL CONSTRAINT PK_grade_rules PRIMARY KEY,
        class_id uniqueidentifier NOT NULL,
        subject_id uniqueidentifier NOT NULL,
        min_pass_score decimal(18,2) NOT NULL,
        created_at datetimeoffset(7) NULL,
        updated_at datetimeoffset(7) NULL,
        CONSTRAINT UQ_grade_rules_class_subject UNIQUE (class_id, subject_id)
    );
END

IF OBJECT_ID(N'dbo.grades', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.grades (
        id uniqueidentifier NOT NULL CONSTRAINT PK_grades PRIMARY KEY,
        student_id uniqueidentifier NOT NULL,
        subject_id uniqueidentifier NOT NULL,
        subject_name nvarchar(250) NULL,
        exam_id uniqueidentifier NULL,
        exam_type nvarchar(80) NULL,
        exam_name nvarchar(250) NULL,
        score decimal(18,2) NOT NULL,
        max_score decimal(18,2) NOT NULL,
        percentage decimal(18,2) NULL,
        exam_date date NULL,
        academic_year int NOT NULL,
        semester nvarchar(20) NOT NULL,
        notes nvarchar(max) NULL,
        created_by nvarchar(200) NULL,
        created_at datetimeoffset(7) NOT NULL,
        updated_at datetimeoffset(7) NULL
    );
    CREATE INDEX IX_grades_student ON dbo.grades(student_id, academic_year, semester);
END

IF COL_LENGTH(N'dbo.classes', N'default_min_pass_score') IS NULL
BEGIN
    ALTER TABLE dbo.classes ADD default_min_pass_score decimal(18,2) NOT NULL CONSTRAINT DF_classes_min_pass DEFAULT ((50));
END

IF OBJECT_ID(N'dbo.class_schedule_settings', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.class_schedule_settings (
        id int NOT NULL CONSTRAINT PK_class_schedule_settings PRIMARY KEY,
        day_name nvarchar(50) NOT NULL CONSTRAINT DF_class_schedule_settings_day DEFAULT (N'الأحد'),
        periods_count int NOT NULL CONSTRAINT DF_class_schedule_settings_periods DEFAULT ((6)),
        updated_at datetimeoffset(7) NOT NULL CONSTRAINT DF_class_schedule_settings_updated DEFAULT (sysdatetimeoffset())
    );
    INSERT INTO dbo.class_schedule_settings (id, day_name, periods_count, updated_at)
    VALUES (1, N'الأحد', 6, sysdatetimeoffset());
END

IF OBJECT_ID(N'dbo.class_schedule_periods', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.class_schedule_periods (
        id uniqueidentifier NOT NULL CONSTRAINT PK_class_schedule_periods PRIMARY KEY,
        class_id uniqueidentifier NOT NULL,
        section_id uniqueidentifier NOT NULL,
        day_name nvarchar(50) NOT NULL,
        period_number int NOT NULL,
        subject_id uniqueidentifier NULL,
        duration_minutes int NOT NULL CONSTRAINT DF_class_schedule_duration DEFAULT ((45)),
        created_at datetimeoffset(7) NOT NULL CONSTRAINT DF_class_schedule_created DEFAULT (sysdatetimeoffset()),
        updated_at datetimeoffset(7) NOT NULL CONSTRAINT DF_class_schedule_period_updated DEFAULT (sysdatetimeoffset()),
        CONSTRAINT UQ_class_schedule_slot UNIQUE (class_id, section_id, day_name, period_number)
    );
    CREATE INDEX IX_class_schedule_periods_class ON dbo.class_schedule_periods(class_id, day_name);
END
""";

    public static void EnsureExists(ApplicationDbContext db) =>
        db.Database.ExecuteSqlRaw(Sql);
}
