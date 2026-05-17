using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolsManagement.Api.Data;
using SchoolsManagement.Api.Models.School;

namespace SchoolsManagement.Api.Controllers;

[ApiController]
[Route("api/subjects")]
[AllowAnonymous]
public class SubjectsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public SubjectsController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> List(
        [FromQuery] Guid? classId,
        [FromQuery] Guid? class_id,
        CancellationToken ct)
    {
        var cid = classId ?? class_id;
        var q = _db.Subjects.AsNoTracking();
        if (cid.HasValue)
        {
            q = q.Where(s => s.ClassId == cid.Value);
        }

        var rows = await q.OrderBy(s => s.Name).ToListAsync(ct);
        return Ok(rows.Select(ToDto));
    }

    [HttpPost]
    public async Task<ActionResult<object>> Create([FromBody] UpsertSubjectRequest body, CancellationToken ct)
    {
        if (body.ClassId == Guid.Empty || string.IsNullOrWhiteSpace(body.Name))
        {
            return BadRequest(new { message = "الصف واسم المادة مطلوبان." });
        }

        var now = DateTimeOffset.UtcNow;
        var entity = new SubjectRecord
        {
            Id = Guid.NewGuid(),
            ClassId = body.ClassId,
            Name = body.Name.Trim(),
            TeacherId = body.TeacherId,
            TeacherName = body.TeacherName,
            CreatedAt = now,
            UpdatedAt = now
        };
        _db.Subjects.Add(entity);
        await _db.SaveChangesAsync(ct);
        return Ok(ToDto(entity));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<object>> Update(Guid id, [FromBody] UpsertSubjectRequest body, CancellationToken ct)
    {
        var entity = await _db.Subjects.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (entity is null)
        {
            return NotFound();
        }

        if (!string.IsNullOrWhiteSpace(body.Name))
        {
            entity.Name = body.Name.Trim();
        }

        if (body.ClassId != Guid.Empty)
        {
            entity.ClassId = body.ClassId;
        }

        entity.TeacherId = body.TeacherId;
        entity.TeacherName = body.TeacherName;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Ok(ToDto(entity));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var entity = await _db.Subjects.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (entity is null)
        {
            return NotFound();
        }

        var exams = await _db.Exams.Where(e => e.SubjectId == id).ToListAsync(ct);
        _db.Exams.RemoveRange(exams);
        _db.Subjects.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static object ToDto(SubjectRecord s) => new
    {
        id = s.Id,
        class_id = s.ClassId,
        name = s.Name,
        teacher_id = s.TeacherId,
        teacher_name = s.TeacherName,
        created_at = s.CreatedAt,
        updated_at = s.UpdatedAt
    };
}

public class UpsertSubjectRequest
{
    public Guid ClassId { get; set; }
    public string? Name { get; set; }
    public Guid? TeacherId { get; set; }
    public string? TeacherName { get; set; }
}
