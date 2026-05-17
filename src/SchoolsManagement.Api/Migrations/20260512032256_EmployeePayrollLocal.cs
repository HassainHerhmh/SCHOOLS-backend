using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolsManagement.Api.Migrations
{
    /// <inheritdoc />
    public partial class EmployeePayrollLocal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "employee_absence_settings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    deduction_with_excuse = table.Column<decimal>(type: "decimal(9,2)", nullable: false),
                    deduction_without_excuse = table.Column<decimal>(type: "decimal(9,2)", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_absence_settings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "employee_monthly_processes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    month_name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    start_date = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    end_date = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_monthly_processes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "employees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    password = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Position = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    employee_type = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Specialization = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Subject = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    base_salary = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Allowances = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    responsible_class_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    is_first_login = table.Column<bool>(type: "bit", nullable: false),
                    last_login = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employees", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "employee_monthly_accounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    employee_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    employee_name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    month_name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    base_salary = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Allowances = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    total_deductions = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    total_bonuses = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    total_absence_days = table.Column<int>(type: "int", nullable: false),
                    absence_deduction = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    total_delay_minutes = table.Column<int>(type: "int", nullable: false),
                    delay_deduction = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    total_extra_hours = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    extra_pay = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    deductions_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    bonuses_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    attendance_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    absences_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    delays_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    extra_hours_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    gross_salary = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    net_salary = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    is_paid = table.Column<bool>(type: "bit", nullable: false),
                    paid_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    paid_by = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    payment_method = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_monthly_accounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_employee_monthly_accounts_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_employee_absence_settings_Year_Month",
                table: "employee_absence_settings",
                columns: new[] { "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_employee_monthly_accounts_employee_id_Year_Month",
                table: "employee_monthly_accounts",
                columns: new[] { "employee_id", "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_employee_monthly_processes_Year_Month",
                table: "employee_monthly_processes",
                columns: new[] { "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_employees_Email",
                table: "employees",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "employee_absence_settings");

            migrationBuilder.DropTable(
                name: "employee_monthly_accounts");

            migrationBuilder.DropTable(
                name: "employee_monthly_processes");

            migrationBuilder.DropTable(
                name: "employees");
        }
    }
}
