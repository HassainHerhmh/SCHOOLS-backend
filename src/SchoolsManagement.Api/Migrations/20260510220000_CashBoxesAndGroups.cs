using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolsManagement.Api.Migrations;

/// <inheritdoc />
public partial class CashBoxesAndGroups : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
IF OBJECT_ID(N'dbo.cashbox_groups', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[cashbox_groups] (
        [Id] int NOT NULL IDENTITY(1,1),
        [Code] int NOT NULL,
        [name_ar] nvarchar(500) NOT NULL,
        [name_en] nvarchar(500) NOT NULL DEFAULT (N''),
        [sort_order] int NOT NULL DEFAULT ((0)),
        [branch_id] int NULL,
        CONSTRAINT [PK_cashbox_groups] PRIMARY KEY ([Id])
    );
END
IF OBJECT_ID(N'dbo.cash_boxes', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[cash_boxes] (
        [Id] int NOT NULL IDENTITY(1,1),
        [name_ar] nvarchar(500) NOT NULL,
        [name_en] nvarchar(500) NOT NULL DEFAULT (N''),
        [code] nvarchar(50) NOT NULL,
        [cash_box_group_id] int NULL,
        [parent_account_id] int NULL,
        [account_id] int NULL,
        [branch_id] int NULL,
        [created_by] int NULL,
        CONSTRAINT [PK_cash_boxes] PRIMARY KEY ([Id])
    );
END
""");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
IF OBJECT_ID(N'dbo.cash_boxes', N'U') IS NOT NULL DROP TABLE [dbo].[cash_boxes];
IF OBJECT_ID(N'dbo.cashbox_groups', N'U') IS NOT NULL DROP TABLE [dbo].[cashbox_groups];
""");
    }
}
