-- تشغيل يدوي إذا لزم: إضافة عمود تاريخ ترحيل القيد
IF OBJECT_ID(N'dbo.journal_entries', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.journal_entries', N'posted_at') IS NULL
BEGIN
    ALTER TABLE [dbo].[journal_entries] ADD [posted_at] datetimeoffset(7) NULL;
END;

SET QUOTED_IDENTIFIER ON;
IF OBJECT_ID(N'dbo.journal_entries', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.journal_entries', N'posted_at') IS NOT NULL
BEGIN
    UPDATE [dbo].[journal_entries]
    SET [posted_at] = COALESCE([created_at], [entry_date], SYSUTCDATETIME())
    WHERE [posted_at] IS NULL;
END;
