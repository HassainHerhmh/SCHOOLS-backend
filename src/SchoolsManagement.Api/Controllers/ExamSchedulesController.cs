using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;

using Microsoft.EntityFrameworkCore;

using SchoolsManagement.Api.Data;

using SchoolsManagement.Api.Models.School;



namespace SchoolsManagement.Api.Controllers;



[ApiController]

[Route("api/exam-schedules")]

[AllowAnonymous]

public class ExamSchedulesController : ControllerBase

{

    private static readonly string[] AllowedDays =

    [

        "الأحد",

        "الاثنين",

        "الثلاثاء",

        "الأربعاء",

        "الخميس"

    ];



    private readonly ApplicationDbContext _db;



    public ExamSchedulesController(ApplicationDbContext db) => _db = db;



    [HttpGet]

    public async Task<ActionResult<IEnumerable<object>>> List(

        [FromQuery] Guid? classId,

        [FromQuery] Guid? class_id,

        [FromQuery] string? examMonth,

        [FromQuery] string? exam_month,

        [FromQuery] string? scheduleKind,

        [FromQuery] string? schedule_kind,

        [FromQuery] string? semester,

        CancellationToken ct)

    {

        var cid = classId ?? class_id;

        if (!cid.HasValue || cid.Value == Guid.Empty)

        {

            return BadRequest(new { message = "معرّف الصف مطلوب." });

        }



        var month = NormalizeExamMonth(examMonth ?? exam_month);

        var kindParam = scheduleKind ?? schedule_kind;

        var normalizedSemester = NormalizeSemester(semester);

        var q = _db.ExamSchedules.AsNoTracking().Where(r => r.ClassId == cid.Value);



        if (!string.IsNullOrWhiteSpace(kindParam))

        {

            var kind = NormalizeScheduleKind(kindParam);

            q = q.Where(r => r.ScheduleKind == kind);



            if (kind == "final")

            {

                if (!string.IsNullOrWhiteSpace(normalizedSemester))

                {

                    q = q.Where(r => r.Semester == normalizedSemester);

                }

            }

            else if (!string.IsNullOrWhiteSpace(month))

            {

                q = q.Where(r => r.ExamMonth == month && r.Semester == normalizedSemester);

            }

        }

        else if (!string.IsNullOrWhiteSpace(month))

        {

            q = q.Where(r => r.ExamMonth == month);

        }



        var rows = await q

            .OrderBy(r => r.ExamMonth)

            .ThenBy(r => r.Semester)

            .ThenBy(r => r.SortOrder)

            .ThenBy(r => r.ExamDate)

            .ThenBy(r => r.DayName)

            .ToListAsync(ct);

        return Ok(rows.Select(ToDto));

    }



    [HttpPut("bulk")]

    public async Task<ActionResult<IEnumerable<object>>> SaveBulk(

        [FromBody] BulkExamScheduleRequest body,

        CancellationToken ct)

    {

        if (body.ClassId == Guid.Empty)

        {

            return BadRequest(new { message = "معرّف الصف مطلوب." });

        }



        var kind = NormalizeScheduleKind(body.ScheduleKind);

        var month = NormalizeExamMonth(body.ExamMonth);

        var semester = NormalizeSemester(body.Semester);



        if (kind == "final")

        {

            if (string.IsNullOrWhiteSpace(semester))

            {

                return BadRequest(new { message = "الفصل الدراسي مطلوب لجدول الامتحانات." });

            }

        }

        else if (string.IsNullOrWhiteSpace(month))

        {

            return BadRequest(new { message = "شهر الاختبار مطلوب." });

        }



        var items = (body.Items ?? [])

            .Where(i => !string.IsNullOrWhiteSpace(i.DayName))

            .ToList();



        foreach (var item in items)

        {

            if (!AllowedDays.Contains(item.DayName.Trim()))

            {

                return BadRequest(new { message = $"اليوم غير مسموح: {item.DayName}" });

            }

        }



        var now = DateTimeOffset.UtcNow;

        var existingQuery = _db.ExamSchedules.Where(r => r.ClassId == body.ClassId && r.ScheduleKind == kind);



        if (kind == "final")

        {

            existingQuery = existingQuery.Where(r => r.Semester == semester);

        }

        else

        {

            existingQuery = existingQuery.Where(r => r.ExamMonth == month && r.Semester == semester);

        }



        var existing = await existingQuery.ToListAsync(ct);



        if (existing.Count > 0)

        {

            _db.ExamSchedules.RemoveRange(existing);

        }



        for (var index = 0; index < items.Count; index++)

        {

            var item = items[index];

            var day = item.DayName.Trim();

            var subjectId = item.SubjectId;



            if (!subjectId.HasValue || subjectId.Value == Guid.Empty)

            {

                continue;

            }



            _db.ExamSchedules.Add(new ExamScheduleRecord

            {

                Id = Guid.NewGuid(),

                ClassId = body.ClassId,

                ExamMonth = kind == "final" ? string.Empty : month,

                Semester = semester,

                DayName = day,

                SubjectId = subjectId,

                ExamDate = item.ExamDate,

                DurationMinutes = item.DurationMinutes,

                SortOrder = item.SortOrder ?? index,

                ScheduleKind = kind,

                CreatedAt = now,

                UpdatedAt = now

            });

        }



        await _db.SaveChangesAsync(ct);



        var savedQuery = _db.ExamSchedules.AsNoTracking()

            .Where(r => r.ClassId == body.ClassId && r.ScheduleKind == kind);



        if (kind == "final")

        {

            savedQuery = savedQuery.Where(r => r.Semester == semester);

        }

        else

        {

            savedQuery = savedQuery.Where(r => r.ExamMonth == month && r.Semester == semester);

        }



        var saved = await savedQuery

            .OrderBy(r => r.SortOrder)

            .ThenBy(r => r.ExamDate)

            .ThenBy(r => r.DayName)

            .ToListAsync(ct);



        return Ok(saved.Select(ToDto));

    }



    [HttpDelete("by-month")]

    public async Task<IActionResult> DeleteByMonth(

        [FromQuery] Guid? classId,

        [FromQuery] Guid? class_id,

        [FromQuery] string? examMonth,

        [FromQuery] string? exam_month,

        [FromQuery] string? scheduleKind,

        [FromQuery] string? schedule_kind,

        [FromQuery] string? semester,

        CancellationToken ct)

    {

        var cid = classId ?? class_id;

        if (!cid.HasValue || cid.Value == Guid.Empty)

        {

            return BadRequest(new { message = "معرّف الصف مطلوب." });

        }



        var month = NormalizeExamMonth(examMonth ?? exam_month);

        var kind = NormalizeScheduleKind(scheduleKind ?? schedule_kind);

        var normalizedSemester = NormalizeSemester(semester);



        IQueryable<ExamScheduleRecord> query = _db.ExamSchedules

            .Where(r => r.ClassId == cid.Value && r.ScheduleKind == kind);



        if (kind == "final")

        {

            if (string.IsNullOrWhiteSpace(normalizedSemester))

            {

                return BadRequest(new { message = "الفصل الدراسي مطلوب." });

            }



            query = query.Where(r => r.Semester == normalizedSemester);

        }

        else

        {

            if (string.IsNullOrWhiteSpace(month))

            {

                return BadRequest(new { message = "شهر الاختبار مطلوب." });

            }



            query = query.Where(r => r.ExamMonth == month && r.Semester == normalizedSemester);

        }



        var rows = await query.ToListAsync(ct);



        if (rows.Count == 0)

        {

            return NotFound();

        }



        _db.ExamSchedules.RemoveRange(rows);

        await _db.SaveChangesAsync(ct);

        return NoContent();

    }



    [HttpDelete("bulk")]

    public async Task<IActionResult> DeleteBulk([FromBody] DeleteExamSchedulesRequest body, CancellationToken ct)

    {

        var ids = (body.Ids ?? [])

            .Where(id => id != Guid.Empty)

            .Distinct()

            .ToList();



        if (ids.Count == 0)

        {

            return BadRequest(new { message = "لم يتم تحديد أي صفوف للحذف." });

        }



        var rows = await _db.ExamSchedules.Where(r => ids.Contains(r.Id)).ToListAsync(ct);

        if (rows.Count == 0)

        {

            return NotFound();

        }



        _db.ExamSchedules.RemoveRange(rows);

        await _db.SaveChangesAsync(ct);

        return NoContent();

    }



    private static string NormalizeExamMonth(string? examMonth) =>

        string.IsNullOrWhiteSpace(examMonth) ? string.Empty : examMonth.Trim();



    private static string NormalizeScheduleKind(string? scheduleKind) =>

        string.Equals(scheduleKind, "final", StringComparison.OrdinalIgnoreCase) ? "final" : "quiz";



    private static string NormalizeSemester(string? semester) =>

        string.Equals(semester, "second", StringComparison.OrdinalIgnoreCase) ? "second" : "first";



    private static object ToDto(ExamScheduleRecord row) => new

    {

        id = row.Id,

        class_id = row.ClassId,

        exam_month = row.ExamMonth,

        semester = row.Semester,

        day_name = row.DayName,

        subject_id = row.SubjectId,

        exam_date = row.ExamDate?.ToString("yyyy-MM-dd"),

        duration_minutes = row.DurationMinutes,

        sort_order = row.SortOrder,

        schedule_kind = row.ScheduleKind,

        created_at = row.CreatedAt,

        updated_at = row.UpdatedAt

    };

}



public class BulkExamScheduleRequest

{

    public Guid ClassId { get; set; }

    public string? ExamMonth { get; set; }

    public string? ScheduleKind { get; set; }

    public string? Semester { get; set; }

    public List<ExamScheduleItemRequest>? Items { get; set; }

}



public class ExamScheduleItemRequest

{

    public string DayName { get; set; } = string.Empty;

    public Guid? SubjectId { get; set; }

    public DateOnly? ExamDate { get; set; }

    public int? DurationMinutes { get; set; }

    public int? SortOrder { get; set; }

}



public class DeleteExamSchedulesRequest

{

    public List<Guid>? Ids { get; set; }

}


