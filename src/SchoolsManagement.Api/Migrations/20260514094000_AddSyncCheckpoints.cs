using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolsManagement.Api.Migrations;

/// <inheritdoc />
[Migration("20260514094000_AddSyncCheckpoints")]
public partial class AddSyncCheckpoints : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
IF OBJECT_ID(N'dbo.sync_checkpoints', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[sync_checkpoints] (
        [Key] nvarchar(120) NOT NULL,
        [synced_at] datetimeoffset NOT NULL,
        CONSTRAINT [PK_sync_checkpoints] PRIMARY KEY ([Key])
    );
END
""");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
IF OBJECT_ID(N'dbo.sync_checkpoints', N'U') IS NOT NULL DROP TABLE [dbo].[sync_checkpoints];
""");
    }
}
