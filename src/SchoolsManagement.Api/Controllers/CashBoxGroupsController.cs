using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolsManagement.Api.Data;
using SchoolsManagement.Api.Models.Accounting;

namespace SchoolsManagement.Api.Controllers;

[ApiController]
[Route("api/cashbox-groups")]
[AllowAnonymous]
public class CashBoxGroupsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public CashBoxGroupsController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CashBoxGroupRecord>>> List(CancellationToken cancellationToken)
    {
        var list = await _db.CashBoxGroups.AsNoTracking()
            .OrderBy(g => g.SortOrder)
            .ThenBy(g => g.Id)
            .ToListAsync(cancellationToken);
        return Ok(list);
    }

    [HttpPost]
    public async Task<ActionResult<CashBoxGroupRecord>> Create(
        [FromBody] UpsertAccountGroupRequest body,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(body.NameAr))
        {
            return BadRequest(new { message = "اسم المجموعة (عربي) مطلوب." });
        }

        var entity = new CashBoxGroupRecord
        {
            Code = body.Code,
            NameAr = body.NameAr.Trim(),
            NameEn = body.NameEn?.Trim() ?? string.Empty,
            SortOrder = body.SortOrder,
            BranchId = body.BranchId ?? 1
        };

        _db.CashBoxGroups.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return StatusCode(StatusCodes.Status201Created, entity);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<CashBoxGroupRecord>> Update(
        int id,
        [FromBody] UpsertAccountGroupRequest body,
        CancellationToken cancellationToken)
    {
        var entity = await _db.CashBoxGroups.FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(body.NameAr))
        {
            return BadRequest(new { message = "اسم المجموعة (عربي) مطلوب." });
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
        var entity = await _db.CashBoxGroups.FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        if (await _db.CashBoxes.AnyAsync(c => c.CashBoxGroupId == id, cancellationToken))
        {
            return BadRequest(new { message = "لا يمكن حذف مجموعة مرتبطة بصناديق نقدية." });
        }

        _db.CashBoxGroups.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
