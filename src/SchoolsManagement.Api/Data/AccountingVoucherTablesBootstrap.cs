using Microsoft.EntityFrameworkCore;

namespace SchoolsManagement.Api.Data;

/// <summary>
/// يضمن وجود جداول سندات القبض/الصرف والقيود اليومية ومصارفة العملة في SQL Server حتى لو لم تُسجَّل الهجرة في __EFMigrationsHistory أو فات تطبيقها.
/// يطابق منطق <see cref="Migrations.ReceiptPaymentJournalTables"/>.
/// </summary>
public static class AccountingVoucherTablesBootstrap
{
    private const string Sql = """
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
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_journal_entries_reference' AND object_id = OBJECT_ID(N'dbo.journal_entries'))
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
""";

    public static void EnsureExists(ApplicationDbContext db)
    {
        db.Database.ExecuteSqlRaw(Sql);
    }
}
