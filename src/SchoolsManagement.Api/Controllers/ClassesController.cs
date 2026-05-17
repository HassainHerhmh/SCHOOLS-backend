using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolsManagement.Api.Data;
using SchoolsManagement.Api.Models.School;

namespace SchoolsManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class ClassesController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public ClassesController(ApplicationDbContext db)
    {
        _db = db;
    }

    /// <summary>جميع الصفوف مرتبة حسب ترتيب العرض (للواجهة والقوائم المنسدلة).</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<GradeClass>>> GetAll(CancellationToken cancellationToken)
    {
        var list = await _db.GradeClasses
            .AsNoTracking()
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);
        return Ok(list);
    }

    [HttpPost]
    public async Task<ActionResult<GradeClass>> Create(
        [FromBody] UpsertGradeClassRequest body,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(body.Name))
        {
            return BadRequest(new { message = "اسم الصف مطلوب." });
        }

        var now = DateTimeOffset.UtcNow;
        var entity = new GradeClass
        {
            Id = Guid.NewGuid(),
            Name = body.Name.Trim(),
            Level = body.Level,
            DisplayOrder = body.DisplayOrder,
            TuitionFees = body.TuitionFees,
            UniformFees = body.UniformFees,
            BooksFees = body.BooksFees,
            BusFees = body.BusFees,
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.GradeClasses.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return Created($"/api/classes/{entity.Id}", entity);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<GradeClass>> Update(
        Guid id,
        [FromBody] UpsertGradeClassRequest body,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(body.Name))
        {
            return BadRequest(new { message = "اسم الصف مطلوب." });
        }

        var entity = await _db.GradeClasses.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        entity.Name = body.Name.Trim();
        entity.Level = body.Level;
        entity.DisplayOrder = body.DisplayOrder;
        entity.TuitionFees = body.TuitionFees;
        entity.UniformFees = body.UniformFees;
        entity.BooksFees = body.BooksFees;
        entity.BusFees = body.BusFees;
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return Ok(entity);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _db.GradeClasses.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        _db.GradeClasses.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    /// <summary>رسوم الصف بحسب الاسم كما يفعل الواجه (tuition → school_fee في الواجه).</summary>
    [HttpGet("fees-by-level")]
    public async Task<ActionResult<ClassFeesDto>> GetFeesByLevel([FromQuery] string level, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(level))
        {
            return Ok(new ClassFeesDto(0, 0, 0, 0));
        }

        var row = await _db.GradeClasses
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Name == level, cancellationToken);

        if (row is null)
        {
            return Ok(new ClassFeesDto(0, 0, 0, 0));
        }

        return Ok(new ClassFeesDto(row.TuitionFees, row.UniformFees, row.BooksFees, 0));
    }

    public record ClassFeesDto(decimal TuitionFees, decimal UniformFees, decimal BooksFees, decimal BusFees);
}
