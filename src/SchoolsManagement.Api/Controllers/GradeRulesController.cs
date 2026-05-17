using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolsManagement.Api.Data;
using SchoolsManagement.Api.Models.School;

namespace SchoolsManagement.Api.Controllers;

[ApiController]
[Route("api/grade-rules")]
[AllowAnonymous]
public class GradeRulesController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public GradeRulesController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> List([FromQuery] Guid classId, CancellationToken ct)
    {
        var rules = await _db.GradeRules.AsNoTracking()
            .Where(r => r.ClassId == classId)
            .ToListAsync(ct);
        var subjectIds = rules.Select(r => r.SubjectId).Distinct().ToList();
        var subjects = await _db.Subjects.AsNoTracking()
            .Where(s => subjectIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, ct);

        return Ok(rules.Select(r => new
        {
            id = r.Id,
            class_id = r.ClassId,
            subject_id = r.SubjectId,
            min_pass_score = r.MinPassScore,
            created_at = r.CreatedAt,
            updated_at = r.UpdatedAt,
            subjects = subjects.TryGetValue(r.SubjectId, out var sub)
                ? new { id = sub.Id, name = sub.Name }
                : null
        }));
    }

    [HttpGet("by-subject")]
    public async Task<ActionResult<object>> GetBySubject(
        [FromQuery] Guid classId,
        [FromQuery] Guid subjectId,
        CancellationToken ct)
    {
        var rule = await _db.GradeRules.AsNoTracking()
            .FirstOrDefaultAsync(r => r.ClassId == classId && r.SubjectId == subjectId, ct);
        if (rule is null)
        {
            return Ok(null);
        }

        return Ok(new
        {
            id = rule.Id,
            class_id = rule.ClassId,
            subject_id = rule.SubjectId,
            min_pass_score = rule.MinPassScore
        });
    }

    [HttpPost]
    public async Task<ActionResult<object>> Upsert([FromBody] UpsertGradeRuleRequest body, CancellationToken ct)
    {
        var existing = await _db.GradeRules
            .FirstOrDefaultAsync(r => r.ClassId == body.ClassId && r.SubjectId == body.SubjectId, ct);
        var now = DateTimeOffset.UtcNow;
        if (existing is null)
        {
            existing = new GradeRuleRecord
            {
                Id = Guid.NewGuid(),
                ClassId = body.ClassId,
                SubjectId = body.SubjectId,
                MinPassScore = body.MinPassScore,
                CreatedAt = now,
                UpdatedAt = now
            };
            _db.GradeRules.Add(existing);
        }
        else
        {
            existing.MinPassScore = body.MinPassScore;
            existing.UpdatedAt = now;
        }

        await _db.SaveChangesAsync(ct);
        return Ok(new
        {
            id = existing.Id,
            class_id = existing.ClassId,
            subject_id = existing.SubjectId,
            min_pass_score = existing.MinPassScore
        });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var entity = await _db.GradeRules.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (entity is null)
        {
            return NotFound();
        }

        _db.GradeRules.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpGet("class-default")]
    public async Task<ActionResult<object>> GetClassDefault([FromQuery] Guid classId, CancellationToken ct)
    {
        var cls = await _db.GradeClasses.AsNoTracking().FirstOrDefaultAsync(c => c.Id == classId, ct);
        return Ok(new { default_min_pass_score = cls?.DefaultMinPassScore ?? 50 });
    }

    [HttpPut("class-default")]
    public async Task<IActionResult> UpdateClassDefault([FromBody] UpdateClassDefaultMinPassRequest body, CancellationToken ct)
    {
        var cls = await _db.GradeClasses.FirstOrDefaultAsync(c => c.Id == body.ClassId, ct);
        if (cls is null)
        {
            return NotFound();
        }

        cls.DefaultMinPassScore = body.MinPass;
        cls.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}

public class UpsertGradeRuleRequest
{
    public Guid ClassId { get; set; }
    public Guid SubjectId { get; set; }
    public decimal MinPassScore { get; set; }
}

public class UpdateClassDefaultMinPassRequest
{
    public Guid ClassId { get; set; }
    public decimal MinPass { get; set; }
}
