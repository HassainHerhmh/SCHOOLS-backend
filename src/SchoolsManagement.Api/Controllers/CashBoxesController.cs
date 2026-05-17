using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolsManagement.Api.Data;
using SchoolsManagement.Api.Models.Accounting;

namespace SchoolsManagement.Api.Controllers;

[ApiController]
[Route("api/cash-boxes")]
[AllowAnonymous]
public class CashBoxesController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public CashBoxesController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CashBoxRecord>>> List(CancellationToken cancellationToken)
    {
        var list = await _db.CashBoxes.AsNoTracking()
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);
        return Ok(list);
    }

    [HttpPost]
    public async Task<ActionResult<CashBoxRecord>> Create(
        [FromBody] UpsertCashBoxRequest body,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(body.NameAr))
        {
            return BadRequest(new { message = "اسم الصندوق (عربي) مطلوب." });
        }

        if (string.IsNullOrWhiteSpace(body.Code))
        {
            return BadRequest(new { message = "رمز الصندوق مطلوب." });
        }

        var entity = new CashBoxRecord
        {
            NameAr = body.NameAr.Trim(),
            NameEn = body.NameEn?.Trim() ?? string.Empty,
            Code = body.Code.Trim(),
            CashBoxGroupId = body.CashBoxGroupId,
            ParentAccountId = body.ParentAccountId,
            AccountId = body.AccountId,
            BranchId = body.BranchId ?? 1,
            CreatedBy = body.CreatedBy
        };

        _db.CashBoxes.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return StatusCode(StatusCodes.Status201Created, entity);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<CashBoxRecord>> Update(
        int id,
        [FromBody] UpsertCashBoxRequest body,
        CancellationToken cancellationToken)
    {
        var entity = await _db.CashBoxes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(body.NameAr))
        {
            return BadRequest(new { message = "اسم الصندوق (عربي) مطلوب." });
        }

        if (string.IsNullOrWhiteSpace(body.Code))
        {
            return BadRequest(new { message = "رمز الصندوق مطلوب." });
        }

        entity.NameAr = body.NameAr.Trim();
        entity.NameEn = body.NameEn?.Trim() ?? string.Empty;
        entity.Code = body.Code.Trim();
        entity.CashBoxGroupId = body.CashBoxGroupId;
        entity.ParentAccountId = body.ParentAccountId;
        entity.AccountId = body.AccountId;
        entity.BranchId = body.BranchId ?? entity.BranchId ?? 1;
        entity.CreatedBy = body.CreatedBy ?? entity.CreatedBy;

        await _db.SaveChangesAsync(cancellationToken);
        return Ok(entity);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var entity = await _db.CashBoxes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        _db.CashBoxes.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
