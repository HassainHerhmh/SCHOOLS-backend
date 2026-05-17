using System.Data;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolsManagement.Api.Data;

namespace SchoolsManagement.Api.Controllers;

[ApiController]
[Route("api/database-backup")]
[Authorize]
public class DatabaseBackupController : ControllerBase
{
    private static readonly string[] TableKeys =
    [
        "students", "classes", "sections", "attendance", "employees",
        "employee_monthly_accounts", "employee_absence_settings", "employee_monthly_processes",
        "account_groups", "accountss", "currencies", "currency_exchanges",
        "journal_types", "payment_types", "receipt_types",
        "cashbox_groups", "cash_boxes", "bank_groups", "banks", "transit_accounts_settings",
        "receipt_vouchers", "payment_vouchers", "journal_entries",
        "student_payments", "student_discounts", "student_discount_applications",
        "subjects", "exams", "grade_rules", "grades",
        "transfer_approval_requests",
        "bus_users", "bus_sites", "sync_checkpoints",
        "user_page_permissions"
    ];

    private static readonly Dictionary<string, string> SqlTableNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["students"] = "students",
        ["classes"] = "classes",
        ["sections"] = "sections",
        ["attendance"] = "attendance",
        ["employees"] = "employees",
        ["employee_monthly_accounts"] = "employee_monthly_accounts",
        ["employee_absence_settings"] = "employee_absence_settings",
        ["employee_monthly_processes"] = "employee_monthly_processes",
        ["account_groups"] = "account_groups",
        ["accountss"] = "accountss",
        ["currencies"] = "currencies",
        ["currency_exchanges"] = "currency_exchanges",
        ["journal_types"] = "journal_types",
        ["payment_types"] = "payment_types",
        ["receipt_types"] = "receipt_types",
        ["cashbox_groups"] = "cashbox_groups",
        ["cash_boxes"] = "cash_boxes",
        ["bank_groups"] = "bank_groups",
        ["banks"] = "banks",
        ["transit_accounts_settings"] = "transit_accounts_settings",
        ["receipt_vouchers"] = "receipt_vouchers",
        ["payment_vouchers"] = "payment_vouchers",
        ["journal_entries"] = "journal_entries",
        ["student_payments"] = "student_payments",
        ["student_discounts"] = "student_discounts",
        ["student_discount_applications"] = "student_discount_applications",
        ["subjects"] = "subjects",
        ["exams"] = "exams",
        ["grade_rules"] = "grade_rules",
        ["grades"] = "grades",
        ["transfer_approval_requests"] = "transfer_approval_requests",
        ["bus_users"] = "bus_users",
        ["bus_sites"] = "bus_sites",
        ["sync_checkpoints"] = "sync_checkpoints",
        ["user_page_permissions"] = "user_page_permissions",
        ["payments"] = "student_payments"
    };

    private readonly ApplicationDbContext _db;

    public DatabaseBackupController(ApplicationDbContext db) => _db = db;

    [HttpGet("tables")]
    public ActionResult<IEnumerable<string>> Tables() => Ok(TableKeys);

    [HttpGet("export/{tableKey}")]
    public async Task<ActionResult<IEnumerable<JsonElement>>> Export(string tableKey, CancellationToken ct)
    {
        if (!SqlTableNames.TryGetValue(tableKey, out var sqlName))
        {
            return NotFound(new { message = "جدول غير معروف." });
        }

        var conn = _db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
        {
            await conn.OpenAsync(ct);
        }

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT * FROM dbo.[{sqlName.Replace("]", "]]")}]";
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var rows = new List<Dictionary<string, object?>>();
        while (await reader.ReadAsync(ct))
        {
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < reader.FieldCount; i++)
            {
                var val = reader.GetValue(i);
                row[reader.GetName(i)] = val == DBNull.Value ? null : val;
            }

            rows.Add(row);
        }

        var json = JsonSerializer.SerializeToElement(rows);
        if (json.ValueKind == JsonValueKind.Array)
        {
            return Ok(json.EnumerateArray().ToList());
        }

        return Ok(Array.Empty<JsonElement>());
    }
}
