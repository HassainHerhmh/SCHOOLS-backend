/*
  إصلاح: Invalid column name 'UserType'
  نفّذ هذا على قاعدتك (مثل SchoolsDb — نفس ConnectionString في appsettings).

  بعد التنفيذ: أعد تحميل صفحة المستخدمين أو أعد تشغيل الـ API.
*/

SET NOCOUNT ON;

IF COL_LENGTH(N'dbo.AspNetUsers', N'UserType') IS NULL
BEGIN
    ALTER TABLE dbo.AspNetUsers
    ADD [UserType] nvarchar(64) NOT NULL
        CONSTRAINT DF_AspNetUsers_UserType DEFAULT (N'إداري');

    PRINT N'تمت إضافة العمود UserType إلى AspNetUsers.';
END
ELSE
BEGIN
    PRINT N'العمود UserType موجود مسبقاً — لم يُجرَ تغيير على البنية.';
END

/* مزامنة سجل هجرات EF حتى لا يحاول dotnet ef إعادة نفس الخطوة لاحقاً */
IF NOT EXISTS (
    SELECT 1 FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513000000_AddApplicationUserUserType'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260513000000_AddApplicationUserUserType', N'8.0.0');

    PRINT N'تم تسجيل الهجرة 20260513000000_AddApplicationUserUserType في __EFMigrationsHistory.';
END
ELSE
BEGIN
    PRINT N'سجل الهجرة موجود مسبقاً.';
END
