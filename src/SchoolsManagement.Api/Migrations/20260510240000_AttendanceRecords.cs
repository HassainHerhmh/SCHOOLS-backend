using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolsManagement.Api.Migrations;

/// <inheritdoc />
public partial class AttendanceRecords : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
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
""");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
IF OBJECT_ID(N'dbo.attendance', N'U') IS NOT NULL DROP TABLE [dbo].[attendance];
""");
    }
}
