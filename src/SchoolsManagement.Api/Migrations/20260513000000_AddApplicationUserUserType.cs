using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolsManagement.Api.Migrations;

/// <inheritdoc />
public partial class AddApplicationUserUserType : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "UserType",
            table: "AspNetUsers",
            type: "nvarchar(64)",
            maxLength: 64,
            nullable: false,
            defaultValue: "إداري");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "UserType",
            table: "AspNetUsers");
    }
}
