using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolsManagement.Api.Data;

namespace SchoolsManagement.Api.Controllers;

/// <summary>قراءة بيانات منشورة لتطبيق أولياء الأمور (من سيرفر رويال الخارجي).</summary>
[ApiController]
[Route("api/parents")]
[AllowAnonymous]
public class ParentsAppController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public ParentsAppController(ApplicationDbContext db) => _db = db;

    [HttpGet("students")]
    public async Task<IActionResult> StudentsByParentPhone([FromQuery] string parent_phone, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(parent_phone))
        {
            return BadRequest(new { message = "رقم ولي الأمر مطلوب." });
        }

        var phone = parent_phone.Trim();
        var list = await _db.ParentsStudentSummaries
            .AsNoTracking()
            .Where(s => s.ParentPhone == phone)
            .OrderBy(s => s.Name)
            .ToListAsync(ct);

        return Ok(list);
    }

    /// <summary>تقرير مديونيات الطلاب لولي الأمر (المستحق، المدفوع، الخصم، المتبقي).</summary>
    [HttpGet("student-reports")]
    public async Task<IActionResult> StudentReportsByParentPhone([FromQuery] string parent_phone, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(parent_phone))
        {
            return BadRequest(new { message = "رقم ولي الأمر مطلوب." });
        }

        var phone = parent_phone.Trim();
        var list = await _db.ParentsStudentReports
            .AsNoTracking()
            .Where(r => r.ParentPhone == phone)
            .OrderBy(r => r.Name)
            .ToListAsync(ct);

        return Ok(list);
    }

    [HttpGet("classes")]
    public async Task<IActionResult> Classes(CancellationToken ct)
    {
        var list = await _db.ParentsClassPublishes
            .AsNoTracking()
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(ct);
        return Ok(list);
    }

    [HttpGet("sections")]
    public async Task<IActionResult> Sections([FromQuery] Guid? class_id, CancellationToken ct)
    {
        var query = _db.ParentsSectionPublishes.AsNoTracking();
        if (class_id is not null && class_id != Guid.Empty)
        {
            query = query.Where(s => s.ClassId == class_id);
        }

        var list = await query.OrderBy(s => s.Name).ToListAsync(ct);
        return Ok(list);
    }

    [HttpGet("attendance")]
    public async Task<IActionResult> Attendance([FromQuery] Guid student_id, [FromQuery] int? limit, CancellationToken ct)
    {
        if (student_id == Guid.Empty)
        {
            return BadRequest(new { message = "معرّف الطالب مطلوب." });
        }

        var take = Math.Clamp(limit ?? 60, 1, 365);
        var list = await _db.ParentsAttendanceSummaries
            .AsNoTracking()
            .Where(a => a.StudentId == student_id)
            .OrderByDescending(a => a.Date)
            .Take(take)
            .ToListAsync(ct);

        return Ok(list);
    }
}
