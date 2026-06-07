using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;

using Microsoft.EntityFrameworkCore;

using SchoolsManagement.Api.Data;

using SchoolsManagement.Api.Models.School;



namespace SchoolsManagement.Api.Controllers;



[ApiController]

[Route("api/exams")]

[AllowAnonymous]

public class ExamsController : ControllerBase

{

    private readonly ApplicationDbContext _db;



    private static readonly string[] ArabicMonths =

    [

        "يناير", "فبراير", "مارس", "أبريل", "مايو", "يونيو",

        "يوليو", "أغسطس", "سبتمبر", "أكتوبر", "نوفمبر", "ديسمبر"

    ];



    public ExamsController(ApplicationDbContext db) => _db = db;



    [HttpGet]

    public async Task<ActionResult<IEnumerable<object>>> List(
        [FromQuery] Guid? subjectId,
        [FromQuery] Guid? classId,
        [FromQuery] Guid? class_id,
        CancellationToken ct)

    {

        var q = _db.Exams.AsNoTracking();

        if (subjectId.HasValue)

        {

            q = q.Where(e => e.SubjectId == subjectId.Value);

        }

        var cid = classId ?? class_id;

        if (cid.HasValue)

        {

            q = q.Where(e => _db.Subjects.Any(s => s.Id == e.SubjectId && s.ClassId == cid.Value));

        }



        var rows = await q.OrderByDescending(e => e.ExamDate).ToListAsync(ct);

        return Ok(rows.Select(ToDto));

    }



    [HttpPost]

    public async Task<ActionResult<object>> Create([FromBody] UpsertExamRequest body, CancellationToken ct)

    {

        var title = (body.Title ?? body.Name)?.Trim();

        if (body.SubjectId == Guid.Empty || string.IsNullOrWhiteSpace(title))

        {

            return BadRequest(new { message = "المادة وعنوان الامتحان مطلوبان." });

        }



        var now = DateTimeOffset.UtcNow;

        var academicYear = body.AcademicYear > 0 ? body.AcademicYear : DateTime.UtcNow.Year;

        var examMonth = NormalizeExamMonth(body.ExamMonth, body.ExamDate);

        var entity = new ExamRecord

        {

            Id = Guid.NewGuid(),

            SubjectId = body.SubjectId,

            Title = title!,

            ExamMonth = examMonth,

            ExamDate = ResolveExamDate(examMonth, body.ExamDate, academicYear),

            MaxScore = body.MaxScore > 0 ? body.MaxScore : 100,

            Semester = NormalizeSemester(body.Semester),

            ActivityType = NormalizeActivityType(body.ActivityType),

            AcademicYear = academicYear,

            CreatedAt = now,

            UpdatedAt = now

        };

        _db.Exams.Add(entity);

        await _db.SaveChangesAsync(ct);

        return Ok(ToDto(entity));

    }



    [HttpPut("{id:guid}")]

    public async Task<ActionResult<object>> Update(Guid id, [FromBody] UpsertExamRequest body, CancellationToken ct)

    {

        var entity = await _db.Exams.FirstOrDefaultAsync(e => e.Id == id, ct);

        if (entity is null)

        {

            return NotFound();

        }



        var title = (body.Title ?? body.Name)?.Trim();

        if (!string.IsNullOrWhiteSpace(title))

        {

            entity.Title = title!;

        }



        if (body.SubjectId != Guid.Empty)

        {

            entity.SubjectId = body.SubjectId;

        }



        if (!string.IsNullOrWhiteSpace(body.ExamMonth))

        {

            entity.ExamMonth = NormalizeExamMonth(body.ExamMonth, body.ExamDate);

            entity.ExamDate = ResolveExamDate(
                entity.ExamMonth,
                body.ExamDate,
                body.AcademicYear > 0 ? body.AcademicYear : entity.AcademicYear);

        }

        else if (body.ExamDate.HasValue)

        {

            entity.ExamDate = body.ExamDate;

            entity.ExamMonth ??= MonthFromDate(body.ExamDate);

        }



        if (body.MaxScore > 0)

        {

            entity.MaxScore = body.MaxScore;

        }



        if (body.AcademicYear > 0)

        {

            entity.AcademicYear = body.AcademicYear;

        }



        if (!string.IsNullOrWhiteSpace(body.Semester))

        {

            entity.Semester = NormalizeSemester(body.Semester);

        }



        if (!string.IsNullOrWhiteSpace(body.ActivityType))

        {

            entity.ActivityType = NormalizeActivityType(body.ActivityType);

        }



        entity.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);

        return Ok(ToDto(entity));

    }



    [HttpDelete("{id:guid}")]

    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)

    {

        var entity = await _db.Exams.FirstOrDefaultAsync(e => e.Id == id, ct);

        if (entity is null)

        {

            return NotFound();

        }



        _db.Exams.Remove(entity);

        await _db.SaveChangesAsync(ct);

        return NoContent();

    }



    private static object ToDto(ExamRecord e) => new

    {

        id = e.Id,

        subject_id = e.SubjectId,

        title = e.Title,

        name = e.Title,

        exam_date = e.ExamDate?.ToString("yyyy-MM-dd"),

        exam_month = e.ExamMonth ?? MonthFromDate(e.ExamDate),

        max_score = e.MaxScore,

        semester = string.IsNullOrWhiteSpace(e.Semester) ? "first" : e.Semester,

        activity_type = string.IsNullOrWhiteSpace(e.ActivityType) ? "exam" : e.ActivityType,

        academic_year = e.AcademicYear ?? DateTime.UtcNow.Year,

        created_at = e.CreatedAt,

        updated_at = e.UpdatedAt

    };



    private static string NormalizeSemester(string? semester) =>

        string.Equals(semester, "second", StringComparison.OrdinalIgnoreCase) ? "second" : "first";



    private static string NormalizeActivityType(string? activityType) =>

        string.IsNullOrWhiteSpace(activityType) ? "exam" : activityType.Trim();



    private static string? NormalizeExamMonth(string? examMonth, DateOnly? examDate)

    {

        if (!string.IsNullOrWhiteSpace(examMonth))

        {

            return examMonth.Trim();

        }



        return MonthFromDate(examDate);

    }



    private static DateOnly? ResolveExamDate(string? examMonth, DateOnly? examDate, int? academicYear)

    {

        if (!string.IsNullOrWhiteSpace(examMonth))

        {

            var monthIndex = Array.FindIndex(

                ArabicMonths,

                m => string.Equals(m, examMonth.Trim(), StringComparison.Ordinal));

            if (monthIndex >= 0)

            {

                var year = academicYear > 0 ? academicYear.Value : DateTime.UtcNow.Year;

                return new DateOnly(year, monthIndex + 1, 1);

            }

        }



        return examDate;

    }



    private static string? MonthFromDate(DateOnly? examDate)

    {

        if (!examDate.HasValue)

        {

            return null;

        }



        var idx = examDate.Value.Month - 1;

        return idx >= 0 && idx < ArabicMonths.Length ? ArabicMonths[idx] : null;

    }

}



public class UpsertExamRequest

{

    public Guid SubjectId { get; set; }

    public string? Title { get; set; }

    public string? Name { get; set; }

    public DateOnly? ExamDate { get; set; }

    public string? ExamMonth { get; set; }

    public decimal MaxScore { get; set; } = 100;

    public string? Semester { get; set; }

    public string? ActivityType { get; set; }

    public int AcademicYear { get; set; }

}


