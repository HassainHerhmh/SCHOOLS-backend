using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolsManagement.Api.Migrations
{
    /// <inheritdoc />
    public partial class AccountingCurrenciesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "currencies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name_ar = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    symbol = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    exchange_rate = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    min_rate = table.Column<decimal>(type: "decimal(18,6)", nullable: true),
                    max_rate = table.Column<decimal>(type: "decimal(18,6)", nullable: true),
                    is_local = table.Column<bool>(type: "bit", nullable: false),
                    convert_mode = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_currencies", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "currencies");
        }
    }
}
