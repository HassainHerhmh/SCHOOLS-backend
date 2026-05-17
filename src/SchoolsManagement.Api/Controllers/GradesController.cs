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
        if (body.StudentId == Guid.Empty || body.SubjectId == Guid.Empty)
        {
            return BadRequest(new { message = "الطالب والمادة مطلوبان." });
        }

        var percentage = body.MaxScore > 0 ? body.Score / body.MaxScore * 100 : 0;
        GradeRecord? entity = null;
        if (body.Id.HasValue)
        {
            entity = await _db.Grades.FirstOrDefaultAsync(g => g.Id == body.Id.Value, ct);
        }

        if (entity is null && body.ExamId.HasValue)
        {
            entity = await _db.Grades.FirstOrDefaultAsync(
                g => g.StudentId == body.StudentId && g.ExamId == body.ExamId, ct);
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

        entity.StudentId = body.StudentId;
        entity.SubjectId = body.SubjectId;
        entity.SubjectName = body.SubjectName;
        entity.ExamId = body.ExamId;
        entity.ExamType = body.ExamType ?? "exam";
        entity.ExamName = body.ExamName;
        entity.Score = body.Score;
        entity.MaxScore = body.MaxScore;
        entity.Percentage = percentage;
        entity.ExamDate = body.ExamDate;
        entity.AcademicYear = body.AcademicYear;
        entity.Semester = body.Semester ?? "first";
        entity.Notes = body.Notes;
        entity.CreatedBy = body.CreatedBy ?? "المدير";
        entity.UpdatedAt = now;

        await _db.SaveChangesAsync(ct);
        return Ok(ToDto(entity));
    }

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
