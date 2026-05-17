using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolsManagement.Api.Data;
using SchoolsManagement.Api.Models.Accounting;

namespace SchoolsManagement.Api.Controllers;

[ApiController]
[Route("api/banks")]
[AllowAnonymous]
public class BanksController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public BanksController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BankRecord>>> List(CancellationToken cancellationToken)
    {
        var list = await _db.Banks.AsNoTracking()
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);
        return Ok(list);
    }

    [HttpPost]
    public async Task<ActionResult<BankRecord>> Create(
        [FromBody] UpsertBankRequest body,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(body.NameAr))
        {
            return BadRequest(new { message = "اسم البنك (عربي) مطلوب." });
        }

        if (string.IsNullOrWhiteSpace(body.Code))
        {
            return BadRequest(new { message = "رمز البنك مطلوب." });
        }

        var entity = new BankRecord
        {
            NameAr = body.NameAr.Trim(),
            NameEn = body.NameEn?.Trim() ?? string.Empty,
            Code = body.Code.Trim(),
            BankGroupId = body.BankGroupId,
            ParentAccountId = body.ParentAccountId,
            AccountId = body.AccountId,
            BranchId = body.BranchId ?? 1,
            CreatedBy = body.CreatedBy
        };

        _db.Banks.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return StatusCode(StatusCodes.Status201Created, entity);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<BankRecord>> Update(
        int id,
        [FromBody] UpsertBankRequest body,
        CancellationToken cancellationToken)
    {
        var entity = await _db.Banks.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(body.NameAr))
        {
            return BadRequest(new { message = "اسم البنك (عربي) مطلوب." });
        }

        if (string.IsNullOrWhiteSpace(body.Code))
        {
            return BadRequest(new { message = "رمز البنك مطلوب." });
        }

        entity.NameAr = body.NameAr.Trim();
        entity.NameEn = body.NameEn?.Trim() ?? string.Empty;
        entity.Code = body.Code.Trim();
        entity.BankGroupId = body.BankGroupId;
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
        var entity = await _db.Banks.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        _db.Banks.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
