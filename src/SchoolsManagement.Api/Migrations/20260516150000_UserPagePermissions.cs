using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolsManagement.Api.Migrations;

public partial class UserPagePermissions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "user_page_permissions",
            columns: table => new
            {
                id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                user_id = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                permission_key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_user_page_permissions", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_user_page_permissions_user_id",
            table: "user_page_permissions",
            column: "user_id");

        migrationBuilder.CreateIndex(
            name: "IX_user_page_permissions_user_id_permission_key",
            table: "user_page_permissions",
            columns: new[] { "user_id", "permission_key" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "user_page_permissions");
    }
}
