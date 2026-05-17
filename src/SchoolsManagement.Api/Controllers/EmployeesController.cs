using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolsManagement.Api.Data;
using SchoolsManagement.Api.Models.School;
using SchoolsManagement.Api.Services;

namespace SchoolsManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class EmployeesController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _configuration;

    public EmployeesController(ApplicationDbContext db, IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
    }

    private string Pepper => _configuration["EmployeeAuth:Pepper"] ?? "your-secret-key-here";

    private static object MapEmployee(EmployeeRecord e, ChartAccountLookupRow? chart = null)
    {
        return new
        {
            id = e.Id.ToString(),
            name = e.Name,
            email = e.Email,
            phone = e.Phone,
            password = "********",
            position = e.Position,
            employee_type = e.EmployeeType,
            status = e.Status,
            specialization = e.Specialization,
            subject = e.Subject,
            base_salary = e.BaseSalary,
            allowances = e.Allowances,
            responsible_class_id = e.ResponsibleClassId?.ToString(),
            chart_account_id = e.ChartAccountId,
            chart_account_code = chart?.Code,
            chart_account_name_ar = chart?.NameAr,
            is_first_login = e.IsFirstLogin,
            last_login = e.LastLogin,
            created_at = e.CreatedAt,
            updated_at = e.UpdatedAt
        };
    }

    /// <summary>
    /// حساب فرعي تحليلي: موجود في دليل الحسابات، له أب، وليس له حسابات تابعة تحته؛ ولا يُستخدم لموظف آخر.
    /// </summary>
    private async Task<ActionResult?> ValidateEmployeeChartAccountAsync(
        int chartAccountId,
        Guid? excludeEmployeeId,
        CancellationToken ct)
    {
        var chart = await ChartAccountSqlLookup.GetByIdAsync(_db, chartAccountId, ct);
        if (chart is null)
        {
            return BadRequest(new { message = "حساب المحاسبة غير موجود في دليل الحسابات." });
        }

        if (chart.ParentId is null)
        {
            return BadRequest(new { message = "يجب ربط الموظف بحساب فرعي تحت حساب رئيسي، وليس بحساب رئيسي فقط." });
        }

        var hasChildren = await ChartAccountSqlLookup.HasChildAccountsAsync(_db, chart.Id, ct);
        if (hasChildren)
        {
            return BadRequest(new { message = "يجب اختيار حساباً تحليلياً نهائياً (بدون حسابات فرعية تحته)." });
        }

        var taken = await _db.EmployeeRecords.AsNoTracking().AnyAsync(
            e => e.ChartAccountId == chartAccountId && (!excludeEmployeeId.HasValue || e.Id != excludeEmployeeId.Value),
            ct);
        if (taken)
        {
            return Conflict(new { message = "هذا الحساب المحاسبي مرتبط بموظف آخر بالفعل." });
        }

        return null;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> List(CancellationToken ct)
    {
        var rows = await _db.EmployeeRecords
            .AsNoTracking()
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(ct);
        var chartIds = rows.Where(r => r.ChartAccountId.HasValue).Select(r => r.ChartAccountId!.Value).Distinct().ToList();
        var charts = chartIds.Count == 0
            ? new Dictionary<int, ChartAccountLookupRow>()
            : await ChartAccountSqlLookup.GetByIdsAsync(_db, chartIds, ct);

        return Ok(rows.Select(e =>
            MapEmployee(e, e.ChartAccountId is int cid && charts.TryGetValue(cid, out var ch) ? ch : null)));
    }

    public class EmployeeLoginBody
    {
        public string Email { get; set; } = "";

        public string Password { get; set; } = "";
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<object>> GetById(Guid id, CancellationToken ct)
    {
        var row = await _db.EmployeeRecords.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, ct);
        if (row is null)
        {
            return NotFound();
        }

        ChartAccountLookupRow? chart = null;
        if (row.ChartAccountId is int cid)
        {
            chart = await ChartAccountSqlLookup.GetByIdAsync(_db, cid, ct);
        }

        return Ok(MapEmployee(row, chart));
    }

    [HttpPost("login")]
    public async Task<ActionResult<object>> Login([FromBody] EmployeeLoginBody body, CancellationToken ct)
    {
        if (body is null)
        {
            return Ok(new { success = false, message = "أدخل البريد الإلكتروني وكلمة المرور." });
        }

        var email = (body.Email ?? "").Trim().ToLowerInvariant();
        var employee = await _db.EmployeeRecords.FirstOrDefaultAsync(
            e => e.Email.ToLower() == email, ct);
        if (employee is null)
        {
            return Ok(new { success = false, message = "البريد الإلكتروني غير موجود" });
        }

        var hash = EmployeePasswordHasher.Hash(body.Password ?? "", Pepper);
        if (!string.Equals(employee.PasswordHash, hash, StringComparison.OrdinalIgnoreCase))
        {
            return Ok(new { success = false, message = "كلمة المرور غير صحيحة" });
        }

        if (employee.Status != "active")
        {
            return Ok(new { success = false, message = "الحساب غير نشط. يرجى التواصل مع الإدارة" });
        }

        var now = DateTimeOffset.UtcNow;
        employee.LastLogin = now;
        employee.IsFirstLogin = false;
        employee.UpdatedAt = now;
        await _db.SaveChangesAsync(ct);

        ChartAccountLookupRow? loginChart = null;
        if (employee.ChartAccountId is int lid)
        {
            loginChart = await ChartAccountSqlLookup.GetByIdAsync(_db, lid, ct);
        }

        return Ok(new
        {
            success = true,
            message = "تم تسجيل الدخول بنجاح",
            employee = MapEmployee(employee, loginChart)
        });
    }

    public class UpsertEmployeeBody
    {
        public string Name { get; set; } = "";

        public string Email { get; set; } = "";

        public string? Phone { get; set; }

        [JsonPropertyName("password_plain")]
        public string? PasswordPlain { get; set; }

        public string? Position { get; set; }

        [JsonPropertyName("employee_type")]
        public string? EmployeeType { get; set; }

        public string? Specialization { get; set; }

        public string? Subject { get; set; }

        [JsonPropertyName("base_salary")]
        public decimal BaseSalary { get; set; }

        public decimal Allowances { get; set; }

        [JsonPropertyName("chart_account_id")]
        public int? ChartAccountId { get; set; }
    }

    [HttpPost]
    public async Task<ActionResult<object>> Create([FromBody] UpsertEmployeeBody body, CancellationToken ct)
    {
        if (body is null)
        {
            return BadRequest(new { message = "بيانات الطلب فارغة." });
        }

        var email = (body.Email ?? "").Trim().ToLowerInvariant();
        var nameTrimmed = (body.Name ?? "").Trim();
        if (string.IsNullOrWhiteSpace(nameTrimmed) || string.IsNullOrWhiteSpace(email))
        {
            return BadRequest(new { message = "الاسم والبريد مطلوبان." });
        }

        var exists = await _db.EmployeeRecords.AnyAsync(e => e.Email.ToLower() == email, ct);
        if (exists)
        {
            return Conflict(new { message = "البريد الإلكتروني مستخدم بالفعل." });
        }

        if (!body.ChartAccountId.HasValue || body.ChartAccountId.Value <= 0)
        {
            return BadRequest(new { message = "يجب ربط الموظف بحساب محاسبي فرعي من دليل الحسابات." });
        }

        var chartErr = await ValidateEmployeeChartAccountAsync(body.ChartAccountId.Value, null, ct);
        if (chartErr != null)
        {
            return chartErr;
        }

        var now = DateTimeOffset.UtcNow;
        var passwordPlain = body.PasswordPlain;
        if (string.IsNullOrWhiteSpace(passwordPlain))
        {
            passwordPlain = "123456";
        }

        var entity = new EmployeeRecord
        {
            Id = Guid.NewGuid(),
            Name = nameTrimmed,
            Email = email,
            Phone = body.Phone,
            PasswordHash = EmployeePasswordHasher.Hash(passwordPlain, Pepper),
            Position = string.IsNullOrWhiteSpace(body.Position) ? "موظف" : body.Position.Trim(),
            EmployeeType = body.EmployeeType is "teacher" or "employee" ? body.EmployeeType! : "employee",
            Status = "active",
            Specialization = body.Specialization,
            Subject = body.Subject,
            BaseSalary = body.BaseSalary,
            Allowances = body.Allowances,
            ChartAccountId = body.ChartAccountId.Value,
            IsFirstLogin = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.EmployeeRecords.Add(entity);
        await _db.SaveChangesAsync(ct);

        var chartCreated =
            await ChartAccountSqlLookup.GetByIdAsync(_db, entity.ChartAccountId!.Value, ct);
        return StatusCode(StatusCodes.Status201Created, MapEmployee(entity, chartCreated));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<object>> Update(Guid id, [FromBody] UpsertEmployeeBody body, CancellationToken ct)
    {
        if (body is null)
        {
            return BadRequest(new { message = "بيانات الطلب فارغة." });
        }

        var entity = await _db.EmployeeRecords.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (entity is null)
        {
            return NotFound(new { message = "لم يتم العثور على الموظف." });
        }

        var email = (body.Email ?? "").Trim().ToLowerInvariant();
        var nameTrimmed = (body.Name ?? "").Trim();
        if (string.IsNullOrWhiteSpace(nameTrimmed) || string.IsNullOrWhiteSpace(email))
        {
            return BadRequest(new { message = "الاسم والبريد مطلوبان." });
        }

        var emailTaken = await _db.EmployeeRecords.AnyAsync(
            e => e.Id != id && e.Email.ToLower() == email, ct);
        if (emailTaken)
        {
            return Conflict(new { message = "البريد الإلكتروني مستخدم لموظف آخر." });
        }

        if (!body.ChartAccountId.HasValue || body.ChartAccountId.Value <= 0)
        {
            return BadRequest(new { message = "يجب ربط الموظف بحساب محاسبي فرعي من دليل الحسابات." });
        }

        var chartErrUpdate = await ValidateEmployeeChartAccountAsync(body.ChartAccountId.Value, id, ct);
        if (chartErrUpdate != null)
        {
            return chartErrUpdate;
        }

        entity.Name = nameTrimmed;
        entity.Email = email;
        entity.Phone = body.Phone;
        entity.Position = string.IsNullOrWhiteSpace(body.Position) ? entity.Position : body.Position.Trim();
        if (!string.IsNullOrWhiteSpace(body.EmployeeType) && body.EmployeeType is "teacher" or "employee")
        {
            entity.EmployeeType = body.EmployeeType;
        }

        entity.Specialization = body.Specialization;
        entity.Subject = body.Subject;
        entity.BaseSalary = body.BaseSalary;
        entity.Allowances = body.Allowances;
        entity.ChartAccountId = body.ChartAccountId.Value;
        if (!string.IsNullOrWhiteSpace(body.PasswordPlain))
        {
            entity.PasswordHash = EmployeePasswordHasher.Hash(body.PasswordPlain, Pepper);
        }

        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);

        var chartUpdated =
            await ChartAccountSqlLookup.GetByIdAsync(_db, entity.ChartAccountId!.Value, ct);
        return Ok(MapEmployee(entity, chartUpdated));
    }

    public class SalaryBody
    {
        [JsonPropertyName("base_salary")]
        public decimal BaseSalary { get; set; }
    }

    [HttpPut("{id:guid}/base-salary")]
    public async Task<ActionResult<object>> UpdateSalary(Guid id, [FromBody] SalaryBody body, CancellationToken ct)
    {
        var entity = await _db.EmployeeRecords.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (entity is null)
        {
            return NotFound(new { message = "لم يتم العثور على الموظف." });
        }

        entity.BaseSalary = body.BaseSalary;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);

        ChartAccountLookupRow? salaryChart = null;
        if (entity.ChartAccountId is int sid)
        {
            salaryChart = await ChartAccountSqlLookup.GetByIdAsync(_db, sid, ct);
        }

        return Ok(MapEmployee(entity, salaryChart));
    }

    public class ResetPasswordBody
    {
        [JsonPropertyName("new_password")]
        public string NewPassword { get; set; } = "";
    }

    [HttpPut("{id:guid}/reset-password")]
    public async Task<IActionResult> ResetPassword(Guid id, [FromBody] ResetPasswordBody body, CancellationToken ct)
    {
        var entity = await _db.EmployeeRecords.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (entity is null)
        {
            return NotFound(new { message = "لم يتم العثور على الموظف." });
        }

        entity.PasswordHash = EmployeePasswordHasher.Hash(body.NewPassword, Pepper);
        entity.IsFirstLogin = true;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);

        return NoContent();
    }

    public class ChangePasswordBody
    {
        [JsonPropertyName("old_password")]
        public string OldPassword { get; set; } = "";

        [JsonPropertyName("new_password")]
        public string NewPassword { get; set; } = "";
    }

    [HttpPost("{id:guid}/change-password")]
    public async Task<ActionResult<object>> ChangePassword(Guid id, [FromBody] ChangePasswordBody body, CancellationToken ct)
    {
        var entity = await _db.EmployeeRecords.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (entity is null)
        {
            return NotFound(new { success = false, message = "الموظف غير موجود." });
        }

        var oldHash = EmployeePasswordHasher.Hash(body.OldPassword, Pepper);
        if (!string.Equals(entity.PasswordHash, oldHash, StringComparison.OrdinalIgnoreCase))
        {
            return Ok(new { success = false, message = "كلمة المرور الحالية غير صحيحة" });
        }

        entity.PasswordHash = EmployeePasswordHasher.Hash(body.NewPassword, Pepper);
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);

        return Ok(new { success = true, message = "تم تغيير كلمة المرور بنجاح" });
    }

    public class ArchiveBody
    {
        /// <summary>active | inactive | on_leave</summary>
        public string Status { get; set; } = "inactive";
    }

    [HttpPatch("{id:guid}/archive")]
    public async Task<IActionResult> SetStatus(Guid id, [FromBody] ArchiveBody body, CancellationToken ct)
    {
        var entity = await _db.EmployeeRecords.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (entity is null)
        {
            return NotFound();
        }

        entity.Status = string.IsNullOrWhiteSpace(body.Status) ? "inactive" : body.Status;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var entity = await _db.EmployeeRecords.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (entity is null)
        {
            return NotFound();
        }

        _db.EmployeeRecords.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    internal static object MapEmployeeProjection(EmployeeRecord e) => MapEmployee(e);
}
