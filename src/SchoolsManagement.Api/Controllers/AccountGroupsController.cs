using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolsManagement.Api.Data;
using SchoolsManagement.Api.Models.Accounting;

namespace SchoolsManagement.Api.Controllers;

[ApiController]
[Route("api/account-groups")]
[AllowAnonymous]
public class AccountGroupsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public AccountGroupsController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AccountGroupRecord>>> List(CancellationToken cancellationToken)
    {
        var list = await _db.AccountGroups.AsNoTracking()
            .OrderBy(g => g.SortOrder)
            .ThenBy(g => g.Id)
            .ToListAsync(cancellationToken);
        return Ok(list);
    }

    [HttpPost]
    public async Task<ActionResult<AccountGroupRecord>> Create(
        [FromBody] UpsertAccountGroupRequest body,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(body.NameAr))
        {
            return BadRequest(new { message = "اسم المجموعة (عربي) مطلوب." });
        }

        var entity = new AccountGroupRecord
        {
            Code = body.Code,
            NameAr = body.NameAr.Trim(),
            NameEn = body.NameEn?.Trim() ?? string.Empty,
            SortOrder = body.SortOrder,
            BranchId = body.BranchId ?? 1
        };

        _db.AccountGroups.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return StatusCode(StatusCodes.Status201Created, entity);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<AccountGroupRecord>> Update(
        int id,
        [FromBody] UpsertAccountGroupRequest body,
        CancellationToken cancellationToken)
    {
        var entity = await _db.AccountGroups.FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
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
        var entity = await _db.AccountGroups.FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        var hasAccounts = await _db.ChartAccounts.AnyAsync(a => a.AccountGroupId == id, cancellationToken);
        if (hasAccounts)
        {
            return BadRequest(new { message = "لا يمكن حذف مجموعة مرتبطة بحسابات في الدليل." });
        }

        _db.AccountGroups.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
