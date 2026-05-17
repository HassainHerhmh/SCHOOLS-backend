using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolsManagement.Api.Data;

#nullable disable

namespace SchoolsManagement.Api.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260514120000_ReceiptPaymentJournalTables")]
public partial class ReceiptPaymentJournalTables : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
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
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
IF OBJECT_ID(N'dbo.journal_entries', N'U') IS NOT NULL DROP TABLE dbo.journal_entries;
IF OBJECT_ID(N'dbo.payment_vouchers', N'U') IS NOT NULL DROP TABLE dbo.payment_vouchers;
IF OBJECT_ID(N'dbo.receipt_vouchers', N'U') IS NOT NULL DROP TABLE dbo.receipt_vouchers;
""");
    }
}
