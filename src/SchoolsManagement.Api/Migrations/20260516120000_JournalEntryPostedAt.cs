using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolsManagement.Api.Migrations;

/// <inheritdoc />
public partial class JournalEntryPostedAt : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
IF COL_LENGTH(N'dbo.journal_entries', N'posted_at') IS NULL
BEGIN
    ALTER TABLE [dbo].[journal_entries] ADD [posted_at] datetimeoffset(7) NULL;
    UPDATE [dbo].[journal_entries]
    SET [posted_at] = COALESCE([created_at], [entry_date], SYSUTCDATETIME())
    WHERE [posted_at] IS NULL;
END;
""");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
IF COL_LENGTH(N'dbo.journal_entries', N'posted_at') IS NOT NULL
    ALTER TABLE [dbo].[journal_entries] DROP COLUMN [posted_at];
""");
    }
}
