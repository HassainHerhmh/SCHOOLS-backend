using Microsoft.EntityFrameworkCore;

namespace SchoolsManagement.Api.Data;

/// <summary>
/// يضمن عمود posted_at على journal_entries حتى لو لم تُطبَّق الهجرة.
/// </summary>
public static class JournalEntryPostedAtBootstrap
{
    public static void EnsurePostedAtColumn(ApplicationDbContext db)
    {
        db.Database.ExecuteSqlRaw(
            """
IF OBJECT_ID(N'dbo.journal_entries', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.journal_entries', N'posted_at') IS NULL
BEGIN
    ALTER TABLE [dbo].[journal_entries] ADD [posted_at] datetimeoffset(7) NULL;
END;
""");

        db.Database.ExecuteSqlRaw(
            """
SET QUOTED_IDENTIFIER ON;
IF OBJECT_ID(N'dbo.journal_entries', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.journal_entries', N'posted_at') IS NOT NULL
BEGIN
    UPDATE [dbo].[journal_entries]
    SET [posted_at] = COALESCE([created_at], [entry_date], SYSUTCDATETIME())
    WHERE [posted_at] IS NULL;
END;
""");
    }
}
