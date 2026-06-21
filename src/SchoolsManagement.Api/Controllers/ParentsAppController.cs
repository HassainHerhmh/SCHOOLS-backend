using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolsManagement.Api.Data;
using SchoolsManagement.Api.Services;

namespace SchoolsManagement.Api.Controllers;

/// <summary>قراءة بيانات منشورة لتطبيق أولياء الأمور (من سيرفر رويال الخارجي).</summary>
[ApiController]
[Route("api/parents")]
[AllowAnonymous]
public class ParentsAppController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ParentsGradesQueryService _gradesQuery;

    public ParentsAppController(ApplicationDbContext db, ParentsGradesQueryService gradesQuery)
    {
        _db = db;
        _gradesQuery = gradesQuery;
    }

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

    /// <summary>حزمة درجات الطالب (شهري / فصلي / سنوي) مع المواد والاختبارات.</summary>
    [HttpGet("grades-bundle")]
    public async Task<IActionResult> GradesBundle(
        [FromQuery] Guid student_id,
        [FromQuery] int? academic_year,
        CancellationToken ct)
    {
        if (student_id == Guid.Empty)
        {
            return BadRequest(new { message = "معرّف الطالب مطلوب." });
        }

        var bundle = await _gradesQuery.GetBundleAsync(student_id, academic_year, ct);
        return Ok(bundle);
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

    /// <summary>أقساط الرسوم الدراسية أو النقل لطالب (school_fees / bus_fees).</summary>
    [HttpGet("installments")]
    public async Task<IActionResult> Installments(
        [FromQuery] Guid student_id,
        [FromQuery] string? fee_kind,
        [FromQuery] string? feeKind,
        CancellationToken ct)
    {
        if (student_id == Guid.Empty)
        {
            return BadRequest(new { message = "معرّف الطالب مطلوب." });
        }

        var kind = (fee_kind ?? feeKind ?? string.Empty).Trim();
        var query = _db.ParentsStudentInstallments.AsNoTracking()
            .Where(i => i.StudentId == student_id);

        if (!string.IsNullOrEmpty(kind))
        {
            if (kind is not ("school_fees" or "bus_fees"))
            {
                return BadRequest(new { message = "fee_kind يجب أن يكون school_fees أو bus_fees." });
            }

            query = query.Where(i => i.FeeKind == kind);
        }

        var slots = await query
            .OrderBy(i => i.FeeKind)
            .ThenBy(i => i.SlotIndex)
            .Select(i => new
            {
                fee_kind = i.FeeKind,
                index = i.SlotIndex,
                label = i.Label,
                due = i.Due,
                paid = i.Paid,
                remaining = i.Remaining,
                is_fully_paid = i.IsFullyPaid
            })
            .ToListAsync(ct);

        return Ok(slots);
    }

    [HttpGet("schedule/settings")]
    public async Task<IActionResult> ScheduleSettings(CancellationToken ct)
    {
        var row = await _db.ParentsScheduleSettings.AsNoTracking().FirstOrDefaultAsync(s => s.Id == 1, ct);
        if (row is null)
        {
            return Ok(new { day_name = "الأحد", periods_count = 6 });
        }

        return Ok(new { day_name = row.DayName, periods_count = row.PeriodsCount });
    }

    /// <summary>جدول حصص الطالب أو الصف/الشعبة (أحدث تاريخ إن لم يُحدد schedule_date).</summary>
    [HttpGet("schedule")]
    public async Task<IActionResult> Schedule(
        [FromQuery] Guid? student_id,
        [FromQuery] Guid? class_id,
        [FromQuery] Guid? section_id,
        [FromQuery] string? schedule_date,
        [FromQuery] string? day_name,
        CancellationToken ct)
    {
        Guid? classId = class_id;
        Guid? sectionId = section_id;

        if (student_id is { } sid && sid != Guid.Empty)
        {
            var student = await _db.ParentsStudentSummaries.AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == sid, ct);
            if (student is null)
            {
                return NotFound(new { message = "الطالب غير موجود." });
            }

            var resolved = await ResolveClassSectionAsync(student.Level, student.Section, ct);
            if (resolved is null)
            {
                return Ok(Array.Empty<object>());
            }

            classId = resolved.Value.ClassId;
            sectionId = resolved.Value.SectionId;
        }

        if (classId is null || classId == Guid.Empty || sectionId is null || sectionId == Guid.Empty)
        {
            return BadRequest(new { message = "معرّف الطالب أو الصف والشعبة مطلوب." });
        }

        var baseQuery = _db.ParentsSchedulePeriods.AsNoTracking()
            .Where(p => p.ClassId == classId && p.SectionId == sectionId);

        DateOnly? dateFilter = null;
        if (!string.IsNullOrWhiteSpace(schedule_date))
        {
            if (!DateOnly.TryParse(schedule_date.Trim(), out var parsed))
            {
                return BadRequest(new { message = "تاريخ الجدول غير صالح." });
            }

            dateFilter = parsed;
        }
        else if (!string.IsNullOrWhiteSpace(day_name))
        {
            var day = day_name.Trim();
            dateFilter = await baseQuery
                .Where(p => p.DayName == day)
                .MaxAsync(p => (DateOnly?)p.ScheduleDate, ct);
            if (dateFilter is null)
            {
                return Ok(new
                {
                    class_id = classId,
                    section_id = sectionId,
                    schedule_date = (string?)null,
                    entries = Array.Empty<object>(),
                    timeline = Array.Empty<object>()
                });
            }
        }
        else
        {
            dateFilter = await baseQuery.MaxAsync(p => (DateOnly?)p.ScheduleDate, ct);
            if (dateFilter is null)
            {
                return Ok(new
                {
                    class_id = classId,
                    section_id = sectionId,
                    schedule_date = (string?)null,
                    entries = Array.Empty<object>(),
                    timeline = Array.Empty<object>()
                });
            }
        }

        var query = baseQuery.Where(p => p.ScheduleDate == dateFilter.Value);
        if (!string.IsNullOrWhiteSpace(day_name))
        {
            var day = day_name.Trim();
            query = query.Where(p => p.DayName == day);
        }

        var entries = await query
            .OrderBy(p => (p.StartHour ?? 0) * 60 + (p.StartMinute ?? 0))
            .ThenBy(p => p.PeriodNumber)
            .Select(p => new
            {
                id = p.Id,
                kind = p.EntryKind,
                class_id = p.ClassId,
                section_id = p.SectionId,
                section_name = p.SectionName,
                day_name = p.DayName,
                schedule_date = p.ScheduleDate.ToString("yyyy-MM-dd"),
                period_number = p.EntryKind == "custom" ? (int?)null : p.PeriodNumber,
                subject_id = p.SubjectId,
                subject_name = p.SubjectName,
                item_name = p.ItemName,
                duration_minutes = p.DurationMinutes,
                start_hour = p.StartHour,
                start_minute = p.StartMinute,
                end_hour = p.EndHour,
                end_minute = p.EndMinute
            })
            .ToListAsync(ct);

        return Ok(new
        {
            class_id = classId,
            section_id = sectionId,
            schedule_date = dateFilter?.ToString("yyyy-MM-dd"),
            entries,
            timeline = entries
        });
    }

    private async Task<(Guid ClassId, Guid SectionId)?> ResolveClassSectionAsync(
        string level,
        string section,
        CancellationToken ct)
    {
        var levelTrim = (level ?? string.Empty).Trim();
        var sectionTrim = (section ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(levelTrim) || string.IsNullOrEmpty(sectionTrim))
        {
            return null;
        }

        var classRow = await _db.ParentsClassPublishes.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Name == levelTrim || c.Level == levelTrim, ct);
        if (classRow is null)
        {
            return null;
        }

        var sectionRow = await _db.ParentsSectionPublishes.AsNoTracking()
            .FirstOrDefaultAsync(s => s.ClassId == classRow.Id && s.Name == sectionTrim, ct);
        if (sectionRow is null)
        {
            return null;
        }

        return (classRow.Id, sectionRow.Id);
    }
}
