using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolsManagement.Api.Data;
using SchoolsManagement.Api.Models.Accounting;

namespace SchoolsManagement.Api.Controllers;

[ApiController]
[Route("api/journal-types")]
[AllowAnonymous]
public class JournalTypesController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public JournalTypesController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<JournalTypeRecord>>> List(CancellationToken cancellationToken)
    {
        var list = await _db.JournalTypes.AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);
        return Ok(list);
    }

    [HttpPost]
    public async Task<ActionResult<JournalTypeRecord>> Create(
        [FromBody] UpsertAccountGroupRequest body,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(body.NameAr))
        {
            return BadRequest(new { message = "الاسم (عربي) مطلوب." });
        }

        var entity = new JournalTypeRecord
        {
            Code = body.Code,
            NameAr = body.NameAr.Trim(),
            NameEn = body.NameEn?.Trim() ?? string.Empty,
            SortOrder = body.SortOrder,
            BranchId = body.BranchId ?? 1
        };

        _db.JournalTypes.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return StatusCode(StatusCodes.Status201Created, entity);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<JournalTypeRecord>> Update(
        int id,
        [FromBody] UpsertAccountGroupRequest body,
        CancellationToken cancellationToken)
    {
        var entity = await _db.JournalTypes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(body.NameAr))
        {
            return BadRequest(new { message = "الاسم (عربي) مطلوب." });
        }

        entity.Code = body.Code;
        entity.NameAr = body.NameAr.Trim();
        entity.NameEn = body.NameEn?.Trim() ?? string.Empty;
        entity.SortOrder = body.SortOrder;
        entity.BranchId = body.BranchId ?? entity.BranchId ?? 1;

        await _db.SaveChangesAsync(cancellationToken);
        return Ok(entity);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var entity = await _db.JournalTypes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        _db.JournalTypes.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
