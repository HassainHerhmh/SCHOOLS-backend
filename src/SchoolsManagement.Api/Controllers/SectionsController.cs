using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolsManagement.Api.Data;
using SchoolsManagement.Api.Models.School;

namespace SchoolsManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class SectionsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public SectionsController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SectionResponse>>> GetAll(
        [FromQuery(Name = "class_id")] Guid? classId,
        CancellationToken cancellationToken)
    {
        var query = _db.SchoolSections.AsNoTracking().Include(s => s.Class).AsQueryable();
        if (classId.HasValue)
        {
            query = query.Where(s => s.ClassId == classId.Value);
        }

        var entities = await query.OrderBy(s => s.Name).ToListAsync(cancellationToken);
        var list = entities.Select(ToResponse).ToList();

        return Ok(list);
    }

    [HttpPost]
    public async Task<ActionResult<SectionResponse>> Create(
        [FromBody] UpsertSectionRequest body,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(body.Name))
        {
            return BadRequest(new { message = "اسم الشعبة مطلوب." });
        }

        var classExists = await _db.GradeClasses.AnyAsync(c => c.Id == body.ClassId, cancellationToken);
        if (!classExists)
        {
            return BadRequest(new { message = "الصف المحدد غير موجود." });
        }

        var now = DateTimeOffset.UtcNow;
        var entity = new SchoolSection
        {
            Id = Guid.NewGuid(),
            Name = body.Name.Trim(),
            ClassId = body.ClassId,
            TeacherId = ParseOptionalGuid(body.TeacherId),
            TeacherName = string.IsNullOrWhiteSpace(body.TeacherName) ? null : body.TeacherName.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.SchoolSections.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        var withClass = await _db.SchoolSections
            .AsNoTracking()
            .Include(s => s.Class)
            .FirstAsync(s => s.Id == entity.Id, cancellationToken);

        return Created($"/api/sections/{entity.Id}", ToResponse(withClass));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<SectionResponse>> Update(
        Guid id,
        [FromBody] UpsertSectionRequest body,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(body.Name))
        {
            return BadRequest(new { message = "اسم الشعبة مطلوب." });
        }

        var classExists = await _db.GradeClasses.AnyAsync(c => c.Id == body.ClassId, cancellationToken);
        if (!classExists)
        {
            return BadRequest(new { message = "الصف المحدد غير موجود." });
        }

        var entity = await _db.SchoolSections.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        entity.Name = body.Name.Trim();
        entity.ClassId = body.ClassId;
        entity.TeacherId = ParseOptionalGuid(body.TeacherId);
        entity.TeacherName = string.IsNullOrWhiteSpace(body.TeacherName) ? null : body.TeacherName.Trim();
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        var withClass = await _db.SchoolSections
            .AsNoTracking()
            .Include(s => s.Class)
            .FirstAsync(s => s.Id == id, cancellationToken);

        return Ok(ToResponse(withClass));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _db.SchoolSections.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        _db.SchoolSections.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static Guid? ParseOptionalGuid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return Guid.TryParse(value.Trim(), out var g) ? g : null;
    }

    private static SectionResponse ToResponse(SchoolSection s)
    {
        return new SectionResponse
        {
            Id = s.Id,
            Name = s.Name,
            ClassId = s.ClassId,
            ClassName = s.Class?.Name ?? string.Empty,
            TeacherId = s.TeacherId?.ToString(),
            TeacherName = s.TeacherName,
            CreatedAt = s.CreatedAt,
            UpdatedAt = s.UpdatedAt
        };
    }
}
