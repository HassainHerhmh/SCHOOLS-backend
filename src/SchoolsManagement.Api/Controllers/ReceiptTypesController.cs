using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolsManagement.Api.Data;
using SchoolsManagement.Api.Models.Accounting;

namespace SchoolsManagement.Api.Controllers;

[ApiController]
[Route("api/receipt-types")]
[AllowAnonymous]
public class ReceiptTypesController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public ReceiptTypesController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ReceiptTypeRecord>>> List(CancellationToken cancellationToken)
    {
        var list = await _db.ReceiptTypes.AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);
        return Ok(list);
    }

    [HttpPost]
    public async Task<ActionResult<ReceiptTypeRecord>> Create(
        [FromBody] UpsertAccountGroupRequest body,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(body.NameAr))
        {
            return BadRequest(new { message = "الاسم (عربي) مطلوب." });
        }

        var entity = new ReceiptTypeRecord
        {
            Code = body.Code,
            NameAr = body.NameAr.Trim(),
            NameEn = body.NameEn?.Trim() ?? string.Empty,
            SortOrder = body.SortOrder,
            BranchId = body.BranchId ?? 1
        };

        _db.ReceiptTypes.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return StatusCode(StatusCodes.Status201Created, entity);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ReceiptTypeRecord>> Update(
        int id,
        [FromBody] UpsertAccountGroupRequest body,
        CancellationToken cancellationToken)
    {
        var entity = await _db.ReceiptTypes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
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
        var entity = await _db.ReceiptTypes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        _db.ReceiptTypes.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
