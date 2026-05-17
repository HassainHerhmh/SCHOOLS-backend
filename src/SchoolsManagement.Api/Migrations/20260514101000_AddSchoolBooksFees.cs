using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolsManagement.Api.Migrations;

/// <inheritdoc />
[Migration("20260514101000_AddSchoolBooksFees")]
public partial class AddSchoolBooksFees : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
IF COL_LENGTH(N'dbo.classes', N'books_fees') IS NULL
BEGIN
    ALTER TABLE [dbo].[classes]
    ADD [books_fees] decimal(18,2) NOT NULL
        CONSTRAINT [DF_classes_books_fees] DEFAULT 0;
END

IF COL_LENGTH(N'dbo.students', N'books_fees') IS NULL
BEGIN
    ALTER TABLE [dbo].[students]
    ADD [books_fees] decimal(18,2) NOT NULL
        CONSTRAINT [DF_students_books_fees] DEFAULT 0;
END

IF COL_LENGTH(N'dbo.students', N'paid_books_fees') IS NULL
BEGIN
    ALTER TABLE [dbo].[students]
    ADD [paid_books_fees] decimal(18,2) NULL;
END
""");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
IF COL_LENGTH(N'dbo.students', N'paid_books_fees') IS NOT NULL
BEGIN
    ALTER TABLE [dbo].[students] DROP COLUMN [paid_books_fees];
END

IF COL_LENGTH(N'dbo.students', N'books_fees') IS NOT NULL
BEGIN
    ALTER TABLE [dbo].[students] DROP CONSTRAINT IF EXISTS [DF_students_books_fees];
    ALTER TABLE [dbo].[students] DROP COLUMN [books_fees];
END

IF COL_LENGTH(N'dbo.classes', N'books_fees') IS NOT NULL
BEGIN
    ALTER TABLE [dbo].[classes] DROP CONSTRAINT IF EXISTS [DF_classes_books_fees];
    ALTER TABLE [dbo].[classes] DROP COLUMN [books_fees];
END
""");
    }
}
