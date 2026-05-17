using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolsManagement.Api.Migrations
{
    /// <inheritdoc />
    public partial class AccountingChartAndGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "account_groups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<int>(type: "int", nullable: false),
                    name_ar = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    name_en = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    sort_order = table.Column<int>(type: "int", nullable: false),
                    branch_id = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_account_groups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "chart_accounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    name_ar = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    name_en = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    parent_id = table.Column<int>(type: "int", nullable: true),
                    account_group_id = table.Column<int>(type: "int", nullable: true),
                    account_level = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    financial_statement_id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    branch_id = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chart_accounts", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "account_groups");

            migrationBuilder.DropTable(
                name: "chart_accounts");
        }
    }
}
