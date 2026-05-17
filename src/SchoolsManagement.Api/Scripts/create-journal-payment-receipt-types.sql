-- تشغيل يدوي في SSMS على قاعدة SchoolsDb إن لم تُنشأ الجداول بعد (نفس منطق الهجرة JournalPaymentReceiptTypes)
USE [SchoolsDb];
GO

IF OBJECT_ID(N'dbo.journal_types', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[journal_types] (
        [Id] int NOT NULL IDENTITY(1,1),
        [Code] int NOT NULL,
        [name_ar] nvarchar(500) NOT NULL,
        [name_en] nvarchar(500) NOT NULL DEFAULT (N''),
        [sort_order] int NOT NULL DEFAULT ((0)),
        [branch_id] int NULL,
        CONSTRAINT [PK_journal_types] PRIMARY KEY ([Id])
    );
END
GO

IF OBJECT_ID(N'dbo.payment_types', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[payment_types] (
        [Id] int NOT NULL IDENTITY(1,1),
        [Code] int NOT NULL,
        [name_ar] nvarchar(500) NOT NULL,
        [name_en] nvarchar(500) NOT NULL DEFAULT (N''),
        [sort_order] int NOT NULL DEFAULT ((0)),
        [branch_id] int NULL,
        CONSTRAINT [PK_payment_types] PRIMARY KEY ([Id])
    );
END
GO

IF OBJECT_ID(N'dbo.receipt_types', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[receipt_types] (
        [Id] int NOT NULL IDENTITY(1,1),
        [Code] int NOT NULL,
        [name_ar] nvarchar(500) NOT NULL,
        [name_en] nvarchar(500) NOT NULL DEFAULT (N''),
        [sort_order] int NOT NULL DEFAULT ((0)),
        [branch_id] int NULL,
        CONSTRAINT [PK_receipt_types] PRIMARY KEY ([Id])
    );
END
GO
