using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolsManagement.Api.Data;

#nullable disable

namespace SchoolsManagement.Api.Migrations;

/// <inheritdoc />
[DbContext(typeof(ApplicationDbContext))]
[Migration("20260510240000_BanksBankGroupsTransitSettings")]
public class BanksBankGroupsTransitSettings : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
IF OBJECT_ID(N'dbo.bank_groups', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[bank_groups] (
        [Id] int NOT NULL IDENTITY(1,1),
        [Code] int NOT NULL,
        [name_ar] nvarchar(500) NOT NULL,
        [name_en] nvarchar(500) NOT NULL DEFAULT (N''),
        [sort_order] int NOT NULL DEFAULT ((0)),
        [branch_id] int NULL,
        CONSTRAINT [PK_bank_groups] PRIMARY KEY ([Id])
    );
END
IF OBJECT_ID(N'dbo.banks', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[banks] (
        [Id] int NOT NULL IDENTITY(1,1),
        [name_ar] nvarchar(500) NOT NULL,
        [name_en] nvarchar(500) NOT NULL DEFAULT (N''),
        [code] nvarchar(50) NOT NULL,
        [bank_group_id] int NULL,
        [parent_account_id] int NULL,
        [account_id] int NULL,
        [branch_id] int NULL,
        [created_by] int NULL,
        CONSTRAINT [PK_banks] PRIMARY KEY ([Id])
    );
END
IF OBJECT_ID(N'dbo.transit_accounts_settings', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[transit_accounts_settings] (
        [Id] int NOT NULL,
        [student_installments_transit_account] int NULL,
        [courier_commission_account] int NULL,
        [coupon_discount_account] int NULL,
        [transfer_guarantee_account] int NULL,
        [currency_exchange_account] int NULL,
        [customer_guarantee_account] int NULL,
        [customer_credit_account] int NULL,
        [updated_at] datetimeoffset NULL,
        CONSTRAINT [PK_transit_accounts_settings] PRIMARY KEY ([Id])
    );
    INSERT INTO [dbo].[transit_accounts_settings] ([Id]) VALUES (1);
END
""");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
IF OBJECT_ID(N'dbo.transit_accounts_settings', N'U') IS NOT NULL DROP TABLE [dbo].[transit_accounts_settings];
IF OBJECT_ID(N'dbo.banks', N'U') IS NOT NULL DROP TABLE [dbo].[banks];
IF OBJECT_ID(N'dbo.bank_groups', N'U') IS NOT NULL DROP TABLE [dbo].[bank_groups];
""");
    }
}
