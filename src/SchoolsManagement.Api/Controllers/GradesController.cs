using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolsManagement.Api.Data;
using SchoolsManagement.Api.Models.School;

namespace SchoolsManagement.Api.Controllers;

[ApiController]
[Route("api/grades")]
[AllowAnonymous]
public class GradesController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public GradesController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> List(
        [FromQuery] Guid? studentId,
        [FromQuery] Guid? subjectId,
        [FromQuery] int? academicYear,
        [FromQuery] string? semester,
        [FromQuery] int? year,
        CancellationToken ct)
    {
        var q = _db.Grades.AsNoTracking();
        if (studentId.HasValue)
        {
            q = q.Where(g => g.StudentId == studentId.Value);
        }

        if (subjectId.HasValue)
        {
            q = q.Where(g => g.SubjectId == subjectId.Value);
        }

        var ay = academicYear ?? year;
        if (ay.HasValue)
        {
            q = q.Where(g => g.AcademicYear == ay.Value);
        }

        if (!string.IsNullOrWhiteSpace(semester))
        {
            q = q.Where(g => g.Semester == semester);
        }

        var rows = await q.OrderBy(g => g.ExamDate).ToListAsync(ct);
        return Ok(rows.Select(ToDto));
    }

    [HttpPost]
    public async Task<ActionResult<object>> Upsert([FromBody] UpsertGradeRequest body, CancellationToken ct)
    {
        var normalized = await NormalizeAndValidateAsync(body, ct);
        if (normalized.Error is not null)
        {
            return normalized.Error;
        }

        var req = normalized.Request!;
        var percentage = req.MaxScore > 0 ? req.Score / req.MaxScore * 100 : 0;

        GradeRecord? entity = null;
        if (req.Id.HasValue && req.Id != Guid.Empty)
        {
            entity = await _db.Grades.FirstOrDefaultAsync(g => g.Id == req.Id.Value, ct);
            if (entity is not null && entity.StudentId != req.StudentId)
            {
                return BadRequest(new { message = "معرّف الدرجة لا يطابق الطالب." });
            }
        }

        if (entity is null && req.ExamId.HasValue && req.ExamId != Guid.Empty)
        {
            entity = await _db.Grades.FirstOrDefaultAsync(
                g => g.StudentId == req.StudentId && g.ExamId == req.ExamId, ct);
        }

        var now = DateTimeOffset.UtcNow;
        if (entity is null)
        {
            entity = new GradeRecord
            {
                Id = Guid.NewGuid(),
                CreatedAt = now
            };
            _db.Grades.Add(entity);
        }

        entity.StudentId = req.StudentId;
        entity.SubjectId = req.SubjectId;
        entity.SubjectName = req.SubjectName;
        entity.ExamId = req.ExamId;
        entity.ExamType = req.ExamType ?? "exam";
        entity.ExamName = req.ExamName;
        entity.Score = req.Score;
        entity.MaxScore = req.MaxScore;
        entity.Percentage = percentage;
        entity.ExamDate = req.ExamDate;
        entity.AcademicYear = req.AcademicYear;
        entity.Semester = req.Semester ?? "first";
        entity.Notes = req.Notes;
        entity.CreatedBy = req.CreatedBy ?? "المدير";
        entity.UpdatedAt = now;

        await _db.SaveChangesAsync(ct);
        return Ok(ToDto(entity));
    }

    private async Task<(UpsertGradeRequest? Request, ActionResult? Error)> NormalizeAndValidateAsync(
        UpsertGradeRequest body,
        CancellationToken ct)
    {
        if (body.StudentId == Guid.Empty || body.SubjectId == Guid.Empty)
        {
            return (null, BadRequest(new { message = "الطالب والمادة مطلوبان." }));
        }

        var studentExists = await _db.StudentRecords.AsNoTracking()
            .AnyAsync(s => s.Id == body.StudentId, ct);
        if (!studentExists)
        {
            return (null, BadRequest(new { message = "الطالب غير موجود." }));
        }

        var subject = await _db.Subjects.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == body.SubjectId, ct);
        if (subject is null)
        {
            return (null, BadRequest(new { message = "المادة غير موجودة." }));
        }

        ExamRecord? exam = null;
        if (body.ExamId.HasValue && body.ExamId != Guid.Empty)
        {
            exam = await _db.Exams.AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == body.ExamId.Value, ct);
            if (exam is null)
            {
                return (null, BadRequest(new { message = "الاختبار/النشاط غير موجود." }));
            }

            if (exam.SubjectId != body.SubjectId)
            {
                return (null, BadRequest(new { message = "الاختبار لا يتبع المادة المحددة." }));
            }
        }

        var maxScore = exam?.MaxScore ?? body.MaxScore;
        if (maxScore <= 0)
        {
            maxScore = 100;
        }

        var score = body.Score;
        if (score < 0)
        {
            score = 0;
        }

        if (score > maxScore)
        {
            score = maxScore;
        }

        var semester = NormalizeSemester(body.Semester);
        if (exam is not null && !string.IsNullOrWhiteSpace(exam.Semester))
        {
            semester = NormalizeSemester(exam.Semester);
        }

        var academicYear = body.AcademicYear;
        if (exam?.AcademicYear is > 0)
        {
            academicYear = exam.AcademicYear.Value;
        }
        else if (academicYear <= 0)
        {
            academicYear = DateTime.UtcNow.Year;
        }

        body.SubjectName = subject.Name;
        body.Score = score;
        body.MaxScore = maxScore;
        body.Semester = semester;
        body.AcademicYear = academicYear;

        if (exam is not null)
        {
            body.ExamName = exam.Title;
            body.ExamType = string.IsNullOrWhiteSpace(exam.ActivityType) ? "exam" : exam.ActivityType;
            body.ExamDate = exam.ExamDate ?? body.ExamDate;
        }

        return (body, null);
    }

    private static string NormalizeSemester(string? semester) =>
        string.Equals(semester, "second", StringComparison.OrdinalIgnoreCase) ? "second" : "first";

    private static object ToDto(GradeRecord g) => new
    {
        id = g.Id,
        student_id = g.StudentId,
        subject_id = g.SubjectId,
        subject_name = g.SubjectName,
        exam_id = g.ExamId,
        exam_type = g.ExamType,
        exam_name = g.ExamName,
        score = g.Score,
        max_score = g.MaxScore,
        percentage = g.Percentage,
        exam_date = g.ExamDate?.ToString("yyyy-MM-dd"),
        academic_year = g.AcademicYear,
        semester = g.Semester,
        notes = g.Notes,
        created_by = g.CreatedBy,
        created_at = g.CreatedAt,
        updated_at = g.UpdatedAt
    };
}

public class UpsertGradeRequest
{
    public Guid? Id { get; set; }
    public Guid StudentId { get; set; }
    public Guid SubjectId { get; set; }
    public string? SubjectName { get; set; }
    public Guid? ExamId { get; set; }
    public string? ExamType { get; set; }
    public string? ExamName { get; set; }
    public decimal Score { get; set; }
    public decimal MaxScore { get; set; }
    public DateOnly? ExamDate { get; set; }
    public int AcademicYear { get; set; }
    public string? Semester { get; set; }
    public string? Notes { get; set; }
    public string? CreatedBy { get; set; }
}
