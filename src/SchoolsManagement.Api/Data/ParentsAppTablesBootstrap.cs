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

        books_fees decimal(18,2) NOT NULL CONSTRAINT DF_parents_students_books DEFAULT ((0)),

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

        books_fees decimal(18,2) NOT NULL CONSTRAINT DF_parents_classes_books DEFAULT ((0)),

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



IF OBJECT_ID(N'dbo.parents_student_reports', N'U') IS NULL

BEGIN

    CREATE TABLE dbo.parents_student_reports (

        student_id uniqueidentifier NOT NULL CONSTRAINT PK_parents_student_reports PRIMARY KEY,

        parent_phone nvarchar(40) NULL,

        name nvarchar(500) NOT NULL,

        level nvarchar(200) NOT NULL,

        section nvarchar(200) NOT NULL,

        school_fees decimal(18,2) NOT NULL CONSTRAINT DF_parents_reports_school DEFAULT ((0)),

        uniform_fees decimal(18,2) NOT NULL CONSTRAINT DF_parents_reports_uniform DEFAULT ((0)),

        books_fees decimal(18,2) NOT NULL CONSTRAINT DF_parents_reports_books DEFAULT ((0)),

        bus_fees decimal(18,2) NOT NULL CONSTRAINT DF_parents_reports_bus DEFAULT ((0)),

        paid_school_fees decimal(18,2) NOT NULL CONSTRAINT DF_parents_reports_ps DEFAULT ((0)),

        paid_uniform_fees decimal(18,2) NOT NULL CONSTRAINT DF_parents_reports_pu DEFAULT ((0)),

        paid_books_fees decimal(18,2) NOT NULL CONSTRAINT DF_parents_reports_pbk DEFAULT ((0)),

        paid_bus_fees decimal(18,2) NOT NULL CONSTRAINT DF_parents_reports_pbs DEFAULT ((0)),

        total_amount decimal(18,2) NOT NULL CONSTRAINT DF_parents_reports_total DEFAULT ((0)),

        paid_cash_amount decimal(18,2) NOT NULL CONSTRAINT DF_parents_reports_paid DEFAULT ((0)),

        discount_amount decimal(18,2) NOT NULL CONSTRAINT DF_parents_reports_disc DEFAULT ((0)),

        remaining_amount decimal(18,2) NOT NULL CONSTRAINT DF_parents_reports_rem DEFAULT ((0)),

        synced_at datetimeoffset(7) NOT NULL CONSTRAINT DF_parents_reports_synced DEFAULT (sysdatetimeoffset())

    );

    CREATE INDEX IX_parents_student_reports_phone ON dbo.parents_student_reports(parent_phone);

END



IF OBJECT_ID(N'dbo.parents_student_installments', N'U') IS NULL

BEGIN

    CREATE TABLE dbo.parents_student_installments (

        student_id uniqueidentifier NOT NULL,

        fee_kind nvarchar(40) NOT NULL,

        slot_index int NOT NULL,

        label nvarchar(200) NOT NULL,

        due decimal(18,2) NOT NULL CONSTRAINT DF_parents_inst_due DEFAULT ((0)),

        paid decimal(18,2) NOT NULL CONSTRAINT DF_parents_inst_paid DEFAULT ((0)),

        remaining decimal(18,2) NOT NULL CONSTRAINT DF_parents_inst_rem DEFAULT ((0)),

        is_fully_paid bit NOT NULL CONSTRAINT DF_parents_inst_full DEFAULT ((0)),

        synced_at datetimeoffset(7) NOT NULL CONSTRAINT DF_parents_inst_synced DEFAULT (sysdatetimeoffset()),

        CONSTRAINT PK_parents_student_installments PRIMARY KEY (student_id, fee_kind, slot_index)

    );

    CREATE INDEX IX_parents_installments_student ON dbo.parents_student_installments(student_id);

END



IF OBJECT_ID(N'dbo.parents_schedule_periods', N'U') IS NULL

BEGIN

    CREATE TABLE dbo.parents_schedule_periods (

        id uniqueidentifier NOT NULL CONSTRAINT PK_parents_schedule_periods PRIMARY KEY,

        class_id uniqueidentifier NOT NULL,

        section_id uniqueidentifier NOT NULL,

        section_name nvarchar(300) NULL,

        day_name nvarchar(50) NOT NULL,

        schedule_date date NOT NULL,

        period_number int NOT NULL,

        subject_id uniqueidentifier NULL,

        subject_name nvarchar(300) NULL,

        duration_minutes int NOT NULL CONSTRAINT DF_parents_sched_dur DEFAULT ((45)),

        start_hour int NULL,

        start_minute int NULL,

        end_hour int NULL,

        end_minute int NULL,

        synced_at datetimeoffset(7) NOT NULL CONSTRAINT DF_parents_sched_synced DEFAULT (sysdatetimeoffset())

    );

    CREATE INDEX IX_parents_schedule_class_section ON dbo.parents_schedule_periods(class_id, section_id);

    CREATE INDEX IX_parents_schedule_date ON dbo.parents_schedule_periods(schedule_date);

END



IF OBJECT_ID(N'dbo.parents_schedule_settings', N'U') IS NULL

BEGIN

    CREATE TABLE dbo.parents_schedule_settings (

        Id int NOT NULL CONSTRAINT PK_parents_schedule_settings PRIMARY KEY,

        day_name nvarchar(50) NOT NULL CONSTRAINT DF_parents_sched_set_day DEFAULT (N'الأحد'),

        periods_count int NOT NULL CONSTRAINT DF_parents_sched_set_cnt DEFAULT ((6)),

        synced_at datetimeoffset(7) NOT NULL CONSTRAINT DF_parents_sched_set_synced DEFAULT (sysdatetimeoffset())

    );

END



IF OBJECT_ID(N'dbo.parents_schedule_custom_items', N'U') IS NULL

BEGIN

    CREATE TABLE dbo.parents_schedule_custom_items (

        id uniqueidentifier NOT NULL CONSTRAINT PK_parents_schedule_custom_items PRIMARY KEY,

        class_id uniqueidentifier NOT NULL,

        section_id uniqueidentifier NOT NULL,

        section_name nvarchar(300) NULL,

        day_name nvarchar(50) NOT NULL,

        schedule_date date NOT NULL,

        item_name nvarchar(200) NOT NULL,

        position_number int NOT NULL,

        start_hour int NOT NULL,

        start_minute int NOT NULL,

        end_hour int NOT NULL,

        end_minute int NOT NULL,

        synced_at datetimeoffset(7) NOT NULL CONSTRAINT DF_parents_sched_custom_synced DEFAULT (sysdatetimeoffset())

    );

    CREATE INDEX IX_parents_sched_custom_class_section ON dbo.parents_schedule_custom_items(class_id, section_id);

    CREATE INDEX IX_parents_sched_custom_date ON dbo.parents_schedule_custom_items(schedule_date);

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

    books_fees decimal(18,2) NOT NULL DEFAULT 0,

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

    books_fees decimal(18,2) NOT NULL DEFAULT 0,

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



CREATE TABLE IF NOT EXISTS parents_student_reports (

    student_id char(36) NOT NULL,

    parent_phone varchar(40) NULL,

    Name varchar(500) NOT NULL,

    Level varchar(200) NOT NULL,

    Section varchar(200) NOT NULL,

    school_fees decimal(18,2) NOT NULL DEFAULT 0,

    uniform_fees decimal(18,2) NOT NULL DEFAULT 0,

    books_fees decimal(18,2) NOT NULL DEFAULT 0,

    bus_fees decimal(18,2) NOT NULL DEFAULT 0,

    paid_school_fees decimal(18,2) NOT NULL DEFAULT 0,

    paid_uniform_fees decimal(18,2) NOT NULL DEFAULT 0,

    paid_books_fees decimal(18,2) NOT NULL DEFAULT 0,

    paid_bus_fees decimal(18,2) NOT NULL DEFAULT 0,

    total_amount decimal(18,2) NOT NULL DEFAULT 0,

    paid_cash_amount decimal(18,2) NOT NULL DEFAULT 0,

    discount_amount decimal(18,2) NOT NULL DEFAULT 0,

    remaining_amount decimal(18,2) NOT NULL DEFAULT 0,

    synced_at datetime(6) NOT NULL,

    PRIMARY KEY (student_id),

    INDEX IX_parents_student_reports_phone (parent_phone)

);



CREATE TABLE IF NOT EXISTS parents_student_installments (

    student_id char(36) NOT NULL,

    fee_kind varchar(40) NOT NULL,

    slot_index int NOT NULL,

    label varchar(200) NOT NULL,

    due decimal(18,2) NOT NULL DEFAULT 0,

    paid decimal(18,2) NOT NULL DEFAULT 0,

    remaining decimal(18,2) NOT NULL DEFAULT 0,

    is_fully_paid tinyint(1) NOT NULL DEFAULT 0,

    synced_at datetime(6) NOT NULL,

    PRIMARY KEY (student_id, fee_kind, slot_index),

    INDEX IX_parents_installments_student (student_id)

);



CREATE TABLE IF NOT EXISTS parents_schedule_periods (

    id char(36) NOT NULL,

    class_id char(36) NOT NULL,

    section_id char(36) NOT NULL,

    section_name varchar(300) NULL,

    day_name varchar(50) NOT NULL,

    schedule_date date NOT NULL,

    period_number int NOT NULL,

    subject_id char(36) NULL,

    subject_name varchar(300) NULL,

    duration_minutes int NOT NULL DEFAULT 45,

    start_hour int NULL,

    start_minute int NULL,

    end_hour int NULL,

    end_minute int NULL,

    synced_at datetime(6) NOT NULL,

    PRIMARY KEY (id),

    INDEX IX_parents_schedule_class_section (class_id, section_id),

    INDEX IX_parents_schedule_date (schedule_date)

);



CREATE TABLE IF NOT EXISTS parents_schedule_settings (

    Id int NOT NULL,

    day_name varchar(50) NOT NULL DEFAULT 'الأحد',

    periods_count int NOT NULL DEFAULT 6,

    synced_at datetime(6) NOT NULL,

    PRIMARY KEY (Id)

);



CREATE TABLE IF NOT EXISTS parents_schedule_custom_items (

    id char(36) NOT NULL,

    class_id char(36) NOT NULL,

    section_id char(36) NOT NULL,

    section_name varchar(300) NULL,

    day_name varchar(50) NOT NULL,

    schedule_date date NOT NULL,

    item_name varchar(200) NOT NULL,

    position_number int NOT NULL,

    start_hour int NOT NULL,

    start_minute int NOT NULL,

    end_hour int NOT NULL,

    end_minute int NOT NULL,

    synced_at datetime(6) NOT NULL,

    PRIMARY KEY (id),

    INDEX IX_parents_sched_custom_class_section (class_id, section_id),

    INDEX IX_parents_sched_custom_date (schedule_date)

);

""";



    public static void EnsureExists(ApplicationDbContext db)

    {

        if (DatabaseProviderHelper.IsMySql(db))

        {

            db.Database.ExecuteSqlRaw(MySqlSql);

            EnsureBooksFeesColumns(db);

            return;

        }



        db.Database.ExecuteSqlRaw(SqlServerSql);

        EnsureBooksFeesColumns(db);

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

        await EnsureBooksFeesColumnsAsync(db, cancellationToken);

    }

    private const string SqlServerBooksFeesColumnsSql = """

IF COL_LENGTH(N'dbo.parents_students_summary', N'books_fees') IS NULL
    ALTER TABLE dbo.parents_students_summary ADD books_fees decimal(18,2) NOT NULL
        CONSTRAINT DF_parents_students_books DEFAULT ((0));
IF COL_LENGTH(N'dbo.parents_classes', N'books_fees') IS NULL
    ALTER TABLE dbo.parents_classes ADD books_fees decimal(18,2) NOT NULL
        CONSTRAINT DF_parents_classes_books DEFAULT ((0));
""";

    private static void EnsureBooksFeesColumns(ApplicationDbContext db) =>
        db.Database.ExecuteSqlRaw(SqlServerBooksFeesColumnsSql);

    private static async Task EnsureBooksFeesColumnsAsync(ApplicationDbContext db, CancellationToken cancellationToken)
    {
        if (!DatabaseProviderHelper.IsMySql(db))
        {
            EnsureBooksFeesColumns(db);
            return;
        }

        await AddMySqlColumnIfMissingAsync(db, "parents_students_summary", "books_fees", cancellationToken);
        await AddMySqlColumnIfMissingAsync(db, "parents_classes", "books_fees", cancellationToken);
    }

    private static async Task AddMySqlColumnIfMissingAsync(
        ApplicationDbContext db,
        string tableName,
        string columnName,
        CancellationToken cancellationToken)
    {
        var exists = await db.Database.SqlQueryRaw<int>(
                """
                SELECT COUNT(*) AS `Value`
                FROM information_schema.columns
                WHERE table_schema = DATABASE()
                  AND table_name = {0}
                  AND column_name = {1}
                """,
                tableName,
                columnName)
            .FirstOrDefaultAsync(cancellationToken) > 0;

        if (exists)
        {
            return;
        }

        var alterSql = tableName switch
        {
            "parents_students_summary" =>
                "ALTER TABLE parents_students_summary ADD COLUMN books_fees decimal(18,2) NOT NULL DEFAULT 0",
            "parents_classes" =>
                "ALTER TABLE parents_classes ADD COLUMN books_fees decimal(18,2) NOT NULL DEFAULT 0",
            _ => throw new InvalidOperationException($"Unknown parents table: {tableName}")
        };
        await db.Database.ExecuteSqlRawAsync(alterSql, cancellationToken);
    }

}


