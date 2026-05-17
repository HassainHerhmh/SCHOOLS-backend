-- تشغيل يدوي في SSMS على قاعدة SchoolsDb (جدول الحضور والغياب للطلاب)
USE [SchoolsDb];
GO

IF OBJECT_ID(N'dbo.attendance', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[attendance] (
        [Id] int NOT NULL IDENTITY(1,1),
        [student_id] uniqueidentifier NOT NULL,
        [class_id] uniqueidentifier NOT NULL,
        [section] nvarchar(200) NOT NULL,
        [date] date NOT NULL,
        [status] nvarchar(50) NOT NULL,
        [notes] nvarchar(1000) NULL,
        [created_at] datetimeoffset NOT NULL,
        CONSTRAINT [PK_attendance] PRIMARY KEY ([Id])
    );
END
GO
