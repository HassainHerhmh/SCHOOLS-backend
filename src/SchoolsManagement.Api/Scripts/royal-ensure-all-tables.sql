/*
  رويال (SchoolsDb) — إنشاء الجداول والأعمدة الناقصة
  ================================================
  الهدف: تجهيز قاعدة SQL Server قبل رفع/استيراد البيانات عبر API النسخ الاحتياطي
  (GET/POST api/database-backup).

  • آمن للتشغيل أكثر من مرة (IF NOT EXISTS / COL_LENGTH).
  • غيّر اسم القاعدة إن لزم (الافتراضي SchoolsDb).
  • نفّذ في SSMS أو sqlcmd على نفس السيرفر الذي يشير إليه Connection String.

  الجداول الأساسية (students, classes, AspNetUsers, accountss …) يفترض أنها
  موجودة من نسخة .bak أو من dotnet ef database update. هذا السكربت يكمّل
  الجداول الجديدة والأعمدة الإضافية فقط.
*/

USE [SchoolsDb];
GO

SET NOCOUNT ON;
PRINT N'=== رويال: بدء التحقق من الجداول والأعمدة ===';
GO

/* ── 1) أنواع القيود والسندات ── */
IF OBJECT_ID(N'dbo.journal_types', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.journal_types (
        Id int NOT NULL IDENTITY(1,1) CONSTRAINT PK_journal_types PRIMARY KEY,
        Code int NOT NULL,
        name_ar nvarchar(500) NOT NULL,
        name_en nvarchar(500) NOT NULL CONSTRAINT DF_journal_types_name_en DEFAULT (N''),
        sort_order int NOT NULL CONSTRAINT DF_journal_types_sort DEFAULT ((0)),
        branch_id int NULL
    );
END;

IF OBJECT_ID(N'dbo.payment_types', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.payment_types (
        Id int NOT NULL IDENTITY(1,1) CONSTRAINT PK_payment_types PRIMARY KEY,
        Code int NOT NULL,
        name_ar nvarchar(500) NOT NULL,
        name_en nvarchar(500) NOT NULL CONSTRAINT DF_payment_types_name_en DEFAULT (N''),
        sort_order int NOT NULL CONSTRAINT DF_payment_types_sort DEFAULT ((0)),
        branch_id int NULL
    );
END;

IF OBJECT_ID(N'dbo.receipt_types', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.receipt_types (
        Id int NOT NULL IDENTITY(1,1) CONSTRAINT PK_receipt_types PRIMARY KEY,
        Code int NOT NULL,
        name_ar nvarchar(500) NOT NULL,
        name_en nvarchar(500) NOT NULL CONSTRAINT DF_receipt_types_name_en DEFAULT (N''),
        sort_order int NOT NULL CONSTRAINT DF_receipt_types_sort DEFAULT ((0)),
        branch_id int NULL
    );
END;
GO

/* ── 2) صناديق وبنوك وحسابات وسيطة ── */
IF OBJECT_ID(N'dbo.cashbox_groups', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.cashbox_groups (
        Id int NOT NULL IDENTITY(1,1) CONSTRAINT PK_cashbox_groups PRIMARY KEY,
        Code int NOT NULL,
        name_ar nvarchar(500) NOT NULL,
        name_en nvarchar(500) NOT NULL CONSTRAINT DF_cashbox_groups_name_en DEFAULT (N''),
        sort_order int NOT NULL CONSTRAINT DF_cashbox_groups_sort DEFAULT ((0)),
        branch_id int NULL
    );
END;

IF OBJECT_ID(N'dbo.cash_boxes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.cash_boxes (
        Id int NOT NULL IDENTITY(1,1) CONSTRAINT PK_cash_boxes PRIMARY KEY,
        name_ar nvarchar(500) NOT NULL,
        name_en nvarchar(500) NOT NULL CONSTRAINT DF_cash_boxes_name_en DEFAULT (N''),
        code nvarchar(50) NOT NULL,
        cash_box_group_id int NULL,
        parent_account_id int NULL,
        account_id int NULL,
        branch_id int NULL,
        created_by int NULL
    );
END;

IF OBJECT_ID(N'dbo.bank_groups', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.bank_groups (
        Id int NOT NULL IDENTITY(1,1) CONSTRAINT PK_bank_groups PRIMARY KEY,
        Code int NOT NULL,
        name_ar nvarchar(500) NOT NULL,
        name_en nvarchar(500) NOT NULL CONSTRAINT DF_bank_groups_name_en DEFAULT (N''),
        sort_order int NOT NULL CONSTRAINT DF_bank_groups_sort DEFAULT ((0)),
        branch_id int NULL
    );
END;

IF OBJECT_ID(N'dbo.banks', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.banks (
        Id int NOT NULL IDENTITY(1,1) CONSTRAINT PK_banks PRIMARY KEY,
        name_ar nvarchar(500) NOT NULL,
        name_en nvarchar(500) NOT NULL CONSTRAINT DF_banks_name_en DEFAULT (N''),
        code nvarchar(50) NOT NULL,
        bank_group_id int NULL,
        parent_account_id int NULL,
        account_id int NULL,
        branch_id int NULL,
        created_by int NULL
    );
END;

IF OBJECT_ID(N'dbo.transit_accounts_settings', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.transit_accounts_settings (
        Id int NOT NULL CONSTRAINT PK_transit_accounts_settings PRIMARY KEY,
        student_installments_transit_account int NULL,
        courier_commission_account int NULL,
        coupon_discount_account int NULL,
        transfer_guarantee_account int NULL,
        currency_exchange_account int NULL,
        customer_guarantee_account int NULL,
        customer_credit_account int NULL,
        updated_at datetimeoffset NULL
    );
END;
GO

/* ── 3) سندات وقيود ومصارفة عملة ── */
IF OBJECT_ID(N'dbo.receipt_vouchers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.receipt_vouchers (
        Id int NOT NULL CONSTRAINT PK_receipt_vouchers PRIMARY KEY,
        voucher_no nvarchar(80) NOT NULL CONSTRAINT DF_receipt_vouchers_voucher_no DEFAULT (N''),
        voucher_date datetimeoffset(7) NOT NULL,
        receipt_type nvarchar(20) NOT NULL CONSTRAINT DF_receipt_vouchers_receipt_type DEFAULT (N''),
        cash_box_account_id int NULL,
        bank_account_id int NULL,
        transfer_no nvarchar(120) NOT NULL CONSTRAINT DF_receipt_vouchers_transfer_no DEFAULT (N''),
        currency_id int NULL,
        amount decimal(18,2) NOT NULL CONSTRAINT DF_receipt_vouchers_amount DEFAULT ((0)),
        account_id int NULL,
        analytic_account_id nvarchar(200) NOT NULL CONSTRAINT DF_receipt_vouchers_analytic DEFAULT (N''),
        cost_center_id nvarchar(200) NOT NULL CONSTRAINT DF_receipt_vouchers_cost_center DEFAULT (N''),
        journal_type_id int NULL,
        notes nvarchar(max) NOT NULL CONSTRAINT DF_receipt_vouchers_notes DEFAULT (N''),
        handling nvarchar(200) NOT NULL CONSTRAINT DF_receipt_vouchers_handling DEFAULT (N''),
        created_by int NULL,
        branch_id int NULL,
        created_at datetimeoffset(7) NULL
    );
END;

IF OBJECT_ID(N'dbo.payment_vouchers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.payment_vouchers (
        Id int NOT NULL CONSTRAINT PK_payment_vouchers PRIMARY KEY,
        voucher_no nvarchar(80) NOT NULL CONSTRAINT DF_payment_vouchers_voucher_no DEFAULT (N''),
        voucher_date datetimeoffset(7) NOT NULL,
        payment_type nvarchar(20) NOT NULL CONSTRAINT DF_payment_vouchers_payment_type DEFAULT (N''),
        cash_box_account_id int NULL,
        bank_account_id int NULL,
        transfer_no nvarchar(120) NOT NULL CONSTRAINT DF_payment_vouchers_transfer_no DEFAULT (N''),
        currency_id int NULL,
        amount decimal(18,2) NOT NULL CONSTRAINT DF_payment_vouchers_amount DEFAULT ((0)),
        account_id int NULL,
        analytic_account_id nvarchar(200) NOT NULL CONSTRAINT DF_payment_vouchers_analytic DEFAULT (N''),
        cost_center_id nvarchar(200) NOT NULL CONSTRAINT DF_payment_vouchers_cost_center DEFAULT (N''),
        journal_type_id int NULL,
        notes nvarchar(max) NOT NULL CONSTRAINT DF_payment_vouchers_notes DEFAULT (N''),
        handling nvarchar(200) NOT NULL CONSTRAINT DF_payment_vouchers_handling DEFAULT (N''),
        created_by int NULL,
        branch_id int NULL,
        created_at datetimeoffset(7) NULL
    );
END;

IF OBJECT_ID(N'dbo.journal_entries', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.journal_entries (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_journal_entries PRIMARY KEY,
        entry_number int NOT NULL,
        entry_date datetimeoffset(7) NOT NULL,
        description nvarchar(2000) NOT NULL CONSTRAINT DF_journal_entries_description DEFAULT (N''),
        from_account_id int NULL,
        to_account_id int NULL,
        currency_id int NULL,
        amount decimal(18,2) NOT NULL CONSTRAINT DF_journal_entries_amount DEFAULT ((0)),
        reference nvarchar(200) NOT NULL CONSTRAINT DF_journal_entries_reference DEFAULT (N''),
        created_by int NULL,
        branch_id int NULL,
        created_at datetimeoffset(7) NULL
    );
    CREATE UNIQUE INDEX IX_journal_entries_reference
        ON dbo.journal_entries(reference)
        WHERE reference <> N'';
END;

IF COL_LENGTH(N'dbo.journal_entries', N'posted_at') IS NULL
BEGIN
    ALTER TABLE dbo.journal_entries ADD posted_at datetimeoffset(7) NULL;
    UPDATE dbo.journal_entries
    SET posted_at = COALESCE(created_at, entry_date, SYSUTCDATETIME())
    WHERE posted_at IS NULL;
END;

IF OBJECT_ID(N'dbo.currency_exchanges', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.currency_exchanges (
        Id int NOT NULL CONSTRAINT PK_currency_exchanges PRIMARY KEY,
        reference nvarchar(200) NOT NULL CONSTRAINT DF_currency_exchanges_reference DEFAULT (N''),
        exchange_date datetimeoffset(7) NOT NULL,
        exchange_type nvarchar(20) NOT NULL CONSTRAINT DF_currency_exchanges_exchange_type DEFAULT (N''),
        from_currency_id int NULL,
        from_amount decimal(18,2) NOT NULL CONSTRAINT DF_currency_exchanges_from_amount DEFAULT ((0)),
        from_rate decimal(18,6) NOT NULL CONSTRAINT DF_currency_exchanges_from_rate DEFAULT ((0)),
        from_account_id int NULL,
        to_currency_id int NULL,
        to_amount decimal(18,2) NOT NULL CONSTRAINT DF_currency_exchanges_to_amount DEFAULT ((0)),
        to_rate decimal(18,6) NOT NULL CONSTRAINT DF_currency_exchanges_to_rate DEFAULT ((0)),
        to_account_id int NULL,
        customer_name nvarchar(300) NOT NULL CONSTRAINT DF_currency_exchanges_customer DEFAULT (N''),
        notes nvarchar(max) NOT NULL CONSTRAINT DF_currency_exchanges_notes DEFAULT (N''),
        created_by int NULL,
        branch_id int NULL,
        created_at datetimeoffset(7) NULL
    );
END;
GO

/* ── 4) حضور وموظفين ── */
IF OBJECT_ID(N'dbo.attendance', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.attendance (
        Id int NOT NULL IDENTITY(1,1) CONSTRAINT PK_attendance PRIMARY KEY,
        student_id uniqueidentifier NOT NULL,
        class_id uniqueidentifier NOT NULL,
        section nvarchar(200) NOT NULL,
        date date NOT NULL,
        status nvarchar(50) NOT NULL,
        notes nvarchar(1000) NULL,
        created_at datetimeoffset NOT NULL
    );
END;

IF OBJECT_ID(N'dbo.employees', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.employees (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_employees PRIMARY KEY,
        Name nvarchar(500) NOT NULL,
        Email nvarchar(250) NOT NULL,
        Phone nvarchar(40) NULL,
        password nvarchar(500) NOT NULL,
        Position nvarchar(200) NOT NULL,
        employee_type nvarchar(40) NOT NULL,
        Status nvarchar(40) NOT NULL,
        Specialization nvarchar(300) NULL,
        Subject nvarchar(300) NULL,
        base_salary decimal(18,2) NOT NULL,
        Allowances decimal(18,2) NOT NULL,
        responsible_class_id uniqueidentifier NULL,
        is_first_login bit NOT NULL,
        last_login datetimeoffset NULL,
        created_at datetimeoffset NULL,
        updated_at datetimeoffset NULL,
        chart_account_id int NULL
    );
    CREATE UNIQUE INDEX IX_employees_Email ON dbo.employees(Email);
END;

IF COL_LENGTH(N'dbo.employees', N'chart_account_id') IS NULL
BEGIN
    ALTER TABLE dbo.employees ADD chart_account_id int NULL;
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_employees_chart_account_id_unique'
      AND object_id = OBJECT_ID(N'dbo.employees')
)
AND COL_LENGTH(N'dbo.employees', N'chart_account_id') IS NOT NULL
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX IX_employees_chart_account_id_unique
    ON dbo.employees(chart_account_id)
    WHERE chart_account_id IS NOT NULL;
END;

IF OBJECT_ID(N'dbo.employee_absence_settings', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.employee_absence_settings (
        Id int NOT NULL IDENTITY(1,1) CONSTRAINT PK_employee_absence_settings PRIMARY KEY,
        Year int NOT NULL,
        Month int NOT NULL,
        deduction_with_excuse decimal(9,2) NOT NULL,
        deduction_without_excuse decimal(9,2) NOT NULL,
        updated_at datetimeoffset NULL
    );
    CREATE UNIQUE INDEX IX_employee_absence_settings_Year_Month
        ON dbo.employee_absence_settings(Year, Month);
END;

IF OBJECT_ID(N'dbo.employee_monthly_processes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.employee_monthly_processes (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_employee_monthly_processes PRIMARY KEY,
        Year int NOT NULL,
        Month int NOT NULL,
        month_name nvarchar(50) NOT NULL,
        start_date datetimeoffset NULL,
        end_date datetimeoffset NULL,
        Status nvarchar(40) NOT NULL,
        created_at datetimeoffset NULL,
        completed_at datetimeoffset NULL
    );
    CREATE UNIQUE INDEX IX_employee_monthly_processes_Year_Month
        ON dbo.employee_monthly_processes(Year, Month);
END;

IF OBJECT_ID(N'dbo.employee_monthly_accounts', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.employee_monthly_accounts (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_employee_monthly_accounts PRIMARY KEY,
        employee_id uniqueidentifier NOT NULL,
        employee_name nvarchar(500) NOT NULL,
        Year int NOT NULL,
        Month int NOT NULL,
        month_name nvarchar(50) NOT NULL,
        base_salary decimal(18,2) NOT NULL,
        Allowances decimal(18,2) NOT NULL,
        total_deductions decimal(18,2) NOT NULL,
        total_bonuses decimal(18,2) NOT NULL,
        total_absence_days int NOT NULL,
        absence_deduction decimal(18,2) NOT NULL,
        total_delay_minutes int NOT NULL,
        delay_deduction decimal(18,2) NOT NULL,
        total_extra_hours decimal(18,2) NOT NULL,
        extra_pay decimal(18,2) NOT NULL,
        deductions_json nvarchar(max) NOT NULL,
        bonuses_json nvarchar(max) NOT NULL,
        attendance_json nvarchar(max) NOT NULL,
        absences_json nvarchar(max) NOT NULL,
        delays_json nvarchar(max) NOT NULL,
        extra_hours_json nvarchar(max) NOT NULL,
        gross_salary decimal(18,2) NOT NULL,
        net_salary decimal(18,2) NOT NULL,
        Status nvarchar(40) NOT NULL,
        is_paid bit NOT NULL,
        paid_at datetimeoffset NULL,
        paid_by nvarchar(200) NULL,
        payment_method nvarchar(120) NULL,
        Notes nvarchar(2000) NULL,
        created_at datetimeoffset NULL,
        updated_at datetimeoffset NULL,
        CONSTRAINT FK_employee_monthly_accounts_employees
            FOREIGN KEY (employee_id) REFERENCES dbo.employees(Id) ON DELETE CASCADE
    );
    CREATE UNIQUE INDEX IX_employee_monthly_accounts_employee_id_Year_Month
        ON dbo.employee_monthly_accounts(employee_id, Year, Month);
END;
GO

/* ── 5) طلاب: مدفوعات، خصومات، درجات، مواد ── */
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
END;

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
END;

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
END;

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
END;

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
END;

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
END;

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
END;

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
END;

IF COL_LENGTH(N'dbo.classes', N'default_min_pass_score') IS NULL
BEGIN
    ALTER TABLE dbo.classes
    ADD default_min_pass_score decimal(18,2) NOT NULL
        CONSTRAINT DF_classes_min_pass DEFAULT ((50));
END;
GO

/* ── 6) باصات ومزامنة وصلاحيات ── */
IF OBJECT_ID(N'dbo.bus_users', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.bus_users (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_bus_users PRIMARY KEY,
        full_name nvarchar(500) NOT NULL,
        phone_number nvarchar(40) NOT NULL,
        username nvarchar(120) NOT NULL,
        password nvarchar(500) NOT NULL,
        created_at datetimeoffset(7) NULL
    );
    CREATE UNIQUE INDEX IX_bus_users_username ON dbo.bus_users(username);
END;

IF OBJECT_ID(N'dbo.bus_sites', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.bus_sites (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_bus_sites PRIMARY KEY,
        site_name nvarchar(500) NOT NULL,
        fee_amount decimal(14,2) NOT NULL,
        created_at datetimeoffset(7) NULL
    );
    CREATE UNIQUE INDEX IX_bus_sites_site_name ON dbo.bus_sites(site_name);
END;

IF OBJECT_ID(N'dbo.sync_checkpoints', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.sync_checkpoints (
        [Key] nvarchar(120) NOT NULL CONSTRAINT PK_sync_checkpoints PRIMARY KEY,
        synced_at datetimeoffset NOT NULL
    );
END;

IF OBJECT_ID(N'dbo.user_page_permissions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.user_page_permissions (
        id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_user_page_permissions PRIMARY KEY,
        user_id nvarchar(450) NOT NULL,
        permission_key nvarchar(100) NOT NULL,
        CONSTRAINT UQ_user_page_permissions_user_key UNIQUE (user_id, permission_key)
    );
    CREATE INDEX IX_user_page_permissions_user_id ON dbo.user_page_permissions(user_id);
END;
GO

/* ── 7) أعمدة إضافية على المستخدمين ── */
IF COL_LENGTH(N'dbo.AspNetUsers', N'UserType') IS NULL
BEGIN
    ALTER TABLE dbo.AspNetUsers
    ADD UserType nvarchar(64) NOT NULL
        CONSTRAINT DF_AspNetUsers_UserType DEFAULT (N'إداري');
END;

IF COL_LENGTH(N'dbo.AspNetUsers', N'permissions_json') IS NULL
BEGIN
    ALTER TABLE dbo.AspNetUsers ADD permissions_json nvarchar(max) NULL;
END;
GO

/* ── 8) تقرير: الجداول الناقصة من قائمة النسخ الاحتياطي ── */
;WITH required AS (
    SELECT v.table_name
    FROM (VALUES
        (N'students'), (N'classes'), (N'sections'), (N'attendance'), (N'employees'),
        (N'employee_monthly_accounts'), (N'employee_absence_settings'), (N'employee_monthly_processes'),
        (N'account_groups'), (N'accountss'), (N'currencies'), (N'currency_exchanges'),
        (N'journal_types'), (N'payment_types'), (N'receipt_types'),
        (N'cashbox_groups'), (N'cash_boxes'), (N'bank_groups'), (N'banks'), (N'transit_accounts_settings'),
        (N'receipt_vouchers'), (N'payment_vouchers'), (N'journal_entries'),
        (N'student_payments'), (N'student_discounts'), (N'student_discount_applications'),
        (N'subjects'), (N'exams'), (N'grade_rules'), (N'grades'),
        (N'transfer_approval_requests'),
        (N'bus_users'), (N'bus_sites'), (N'sync_checkpoints'),
        (N'user_page_permissions')
    ) AS v(table_name)
)
SELECT r.table_name AS missing_table
FROM required r
LEFT JOIN sys.tables t ON t.name = r.table_name AND t.schema_id = SCHEMA_ID(N'dbo')
WHERE t.object_id IS NULL
ORDER BY r.table_name;

/* ── 9) جداول تطبيق أولياء الأمور (على سيرفر رويال الخارجي) ── */
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
END;

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
END;

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
END;

IF OBJECT_ID(N'dbo.parents_attendance_summary', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.parents_attendance_summary (
        student_id uniqueidentifier NOT NULL,
        date date NOT NULL,
        status nvarchar(50) NOT NULL,
        synced_at datetimeoffset(7) NOT NULL CONSTRAINT DF_parents_attendance_synced DEFAULT (sysdatetimeoffset()),
        CONSTRAINT PK_parents_attendance_summary PRIMARY KEY (student_id, date)
    );
END;
GO

PRINT N'=== انتهى. إن لم يظهر أي صف في النتيجة أعلاه فكل الجداول المطلوبة للرفع موجودة ===';
GO
