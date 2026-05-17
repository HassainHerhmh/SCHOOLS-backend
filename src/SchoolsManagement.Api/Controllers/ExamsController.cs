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

    public ExamsController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> List([FromQuery] Guid? subjectId, CancellationToken ct)
    {
        var q = _db.Exams.AsNoTracking();
        if (subjectId.HasValue)
        {
            q = q.Where(e => e.SubjectId == subjectId.Value);
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
        var entity = new ExamRecord
        {
            Id = Guid.NewGuid(),
            SubjectId = body.SubjectId,
            Title = title!,
            ExamDate = body.ExamDate,
            MaxScore = body.MaxScore > 0 ? body.MaxScore : 100,
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

        if (body.ExamDate.HasValue)
        {
            entity.ExamDate = body.ExamDate;
        }

        if (body.MaxScore > 0)
        {
            entity.MaxScore = body.MaxScore;
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
        max_score = e.MaxScore,
        created_at = e.CreatedAt,
        updated_at = e.UpdatedAt
    };
}

public class UpsertExamRequest
{
    public Guid SubjectId { get; set; }
    public string? Title { get; set; }
    public string? Name { get; set; }
    public DateOnly? ExamDate { get; set; }
    public decimal MaxScore { get; set; } = 100;
}
