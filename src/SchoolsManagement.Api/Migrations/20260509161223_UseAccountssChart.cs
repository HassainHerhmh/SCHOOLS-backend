using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolsManagement.Api.Migrations
{
    /// <inheritdoc />
    public partial class UseAccountssChart : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // الجدول الحقيقي عندكم هو accountss (بيانات مُستوردة). لا نستخدم RenameTable لتجنب التعارض مع accountss القائم.
            migrationBuilder.Sql("""
IF OBJECT_ID(N'dbo.chart_accounts', N'U') IS NOT NULL
    DROP TABLE dbo.chart_accounts;

IF COL_LENGTH(N'dbo.accountss', N'financial_statement_id') IS NOT NULL
BEGIN
    DECLARE @fsLen int;
    SELECT @fsLen = CHARACTER_MAXIMUM_LENGTH
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = N'dbo' AND TABLE_NAME = N'accountss' AND COLUMN_NAME = N'financial_statement_id';
    IF @fsLen IS NOT NULL AND (@fsLen = -1 OR @fsLen < 250)
        ALTER TABLE dbo.accountss ALTER COLUMN financial_statement_id NVARCHAR(250) NULL;
END

IF COL_LENGTH(N'dbo.accountss', N'branch_id') IS NOT NULL
BEGIN
    DECLARE @branchType nvarchar(128);
    SELECT @branchType = DATA_TYPE
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = N'dbo' AND TABLE_NAME = N'accountss' AND COLUMN_NAME = N'branch_id';
    IF @branchType IS NOT NULL AND @branchType <> N'nvarchar'
        ALTER TABLE dbo.accountss ALTER COLUMN branch_id NVARCHAR(500) NULL;
END

IF OBJECT_ID(N'dbo.accountss', N'U') IS NOT NULL
   AND NOT EXISTS (
       SELECT 1 FROM sys.indexes
       WHERE name = N'IX_accountss_id' AND object_id = OBJECT_ID(N'dbo.accountss')
   )
    CREATE UNIQUE NONCLUSTERED INDEX IX_accountss_id ON dbo.accountss(id);
""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
IF EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_accountss_id' AND object_id = OBJECT_ID(N'dbo.accountss')
)
    DROP INDEX IX_accountss_id ON dbo.accountss;
""");

            migrationBuilder.Sql("""
IF OBJECT_ID(N'dbo.chart_accounts', N'U') IS NULL
BEGIN
    CREATE TABLE [chart_accounts] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(100) NOT NULL,
        [name_ar] nvarchar(500) NOT NULL,
        [name_en] nvarchar(500) NOT NULL,
        [parent_id] int NULL,
        [account_group_id] int NULL,
        [account_level] nvarchar(100) NOT NULL,
        [financial_statement_id] nvarchar(250) NULL,
        [created_at] datetimeoffset NOT NULL,
        [created_by] nvarchar(200) NULL,
        [branch_id] nvarchar(500) NULL,
        CONSTRAINT [PK_chart_accounts] PRIMARY KEY ([Id])
    );
END
""");
        }
    }
}
