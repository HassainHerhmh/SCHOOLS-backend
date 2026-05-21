-- جدول تقارير مديونيات الطلاب لتطبيق أولياء الأمور (parents_student_reports)

/* ========== SQL Server ========== */
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
END;

/* ========== MySQL (رويال — Railway) ========== */
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

-- بعد الإنشاء: مزامنة كاملة من المدرسة.
-- قراءة من التطبيق: GET /api/parents/student-reports?parent_phone=...
