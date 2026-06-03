using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolsManagement.Api.Data;
using SchoolsManagement.Api.Models.Accounting;
using SchoolsManagement.Api.Services;

namespace SchoolsManagement.Api.Controllers;

[ApiController]
[Route("api/journal-entries")]
[Authorize]
public class JournalEntriesController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly AccountingCurrentUserService _currentUser;
    private readonly VoucherUserNameEnricher _userNameEnricher;

    public JournalEntriesController(
        ApplicationDbContext db,
        AccountingCurrentUserService currentUser,
        VoucherUserNameEnricher userNameEnricher)
    {
        _db = db;
        _currentUser = currentUser;
        _userNameEnricher = userNameEnricher;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<VoucherJournalEntryRecord>>> List(CancellationToken ct)
    {
        var list = await _db.VoucherJournalEntries.AsNoTracking().OrderByDescending(x => x.EntryNumber).ToListAsync(ct);
        await AccountingVoucherAuditHelper.EnrichDisplayNamesAsync(list, _userNameEnricher, ct);
        return Ok(list);
    }

    [HttpGet("by-reference/{reference}")]
    public async Task<ActionResult<VoucherJournalEntryRecord>> ByReference(string reference, CancellationToken ct)
    {
        var r = Uri.UnescapeDataString(reference ?? "");
        if (string.IsNullOrWhiteSpace(r))
        {
            return BadRequest();
        }

        var row = await _db.VoucherJournalEntries.AsNoTracking().FirstOrDefaultAsync(x => x.Reference == r, ct);
        if (row is null)
        {
            return NotFound();
        }

        await AccountingVoucherAuditHelper.EnrichDisplayNamesAsync([row], _userNameEnricher, ct);
        return Ok(row);
    }

    [HttpGet("next-entry-number")]
    public async Task<ActionResult<object>> NextEntryNumber(CancellationToken ct)
    {
        var max = await _db.VoucherJournalEntries.Select(x => (int?)x.EntryNumber).MaxAsync(ct) ?? 0;
        var next = Math.Max(max, 1000) + 1;
        return Ok(new { next_entry_number = next });
    }

    [HttpPost]
    public async Task<ActionResult<VoucherJournalEntryRecord>> Create([FromBody] VoucherJournalEntryRecord body, CancellationToken ct)
    {
        var id = body.Id == Guid.Empty ? Guid.NewGuid() : body.Id;
        if (await _db.VoucherJournalEntries.AnyAsync(x => x.Id == id, ct))
        {
            return Conflict(new { message = "معرّف القيد مستخدم مسبقاً." });
        }

        var entryNumber = body.EntryNumber <= 0 ? await NextNumberInt(ct) : body.EntryNumber;
        var now = DateTimeOffset.UtcNow;
        var entity = new VoucherJournalEntryRecord
        {
            Id = id,
            EntryNumber = entryNumber,
            EntryDate = body.EntryDate == default ? now : body.EntryDate,
            Description = body.Description ?? "",
            FromAccountId = body.FromAccountId,
            ToAccountId = body.ToAccountId,
            CurrencyId = body.CurrencyId,
            Amount = body.Amount,
            Reference = body.Reference ?? "",
            BranchId = body.BranchId,
            CreatedAt = body.CreatedAt ?? now,
            PostedAt = body.PostedAt ?? now
        };
        await AccountingVoucherAuditHelper.ApplyOnCreateAsync(entity, body, _currentUser, ct);

        if (!string.IsNullOrWhiteSpace(entity.Reference) &&
            await _db.VoucherJournalEntries.AnyAsync(x => x.Reference == entity.Reference, ct))
        {
            return Conflict(new { message = "المرجع مستخدم مسبقاً." });
        }

        _db.VoucherJournalEntries.Add(entity);
        await _db.SaveChangesAsync(ct);
        await AccountingVoucherAuditHelper.EnrichDisplayNamesAsync([entity], _userNameEnricher, ct);
        return CreatedAtAction(nameof(List), new { }, entity);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<VoucherJournalEntryRecord>> Update(Guid id, [FromBody] VoucherJournalEntryRecord body, CancellationToken ct)
    {
        var entity = await _db.VoucherJournalEntries.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null)
        {
            return NotFound();
        }

        if (!string.IsNullOrWhiteSpace(body.Reference) &&
            body.Reference != entity.Reference &&
            await _db.VoucherJournalEntries.AnyAsync(x => x.Reference == body.Reference && x.Id != id, ct))
        {
            return Conflict(new { message = "المرجع مستخدم مسبقاً." });
        }

        entity.EntryDate = body.EntryDate == default ? entity.EntryDate : body.EntryDate;
        entity.Description = body.Description ?? entity.Description;
        entity.FromAccountId = body.FromAccountId;
        entity.ToAccountId = body.ToAccountId;
        entity.CurrencyId = body.CurrencyId;
        entity.Amount = body.Amount;
        entity.Reference = body.Reference ?? "";
        entity.BranchId = body.BranchId;
        entity.CreatedAt = body.CreatedAt ?? entity.CreatedAt;
        await AccountingVoucherAuditHelper.ApplyOnUpdateAsync(entity, body, _currentUser, ct);
        await _db.SaveChangesAsync(ct);
        await AccountingVoucherAuditHelper.EnrichDisplayNamesAsync([entity], _userNameEnricher, ct);
        return Ok(entity);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var entity = await _db.VoucherJournalEntries.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null)
        {
            return NotFound();
        }

        _db.VoucherJournalEntries.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    private async Task<int> NextNumberInt(CancellationToken ct)
    {
        var max = await _db.VoucherJournalEntries.Select(x => (int?)x.EntryNumber).MaxAsync(ct) ?? 0;
        return Math.Max(max, 1000) + 1;
    }
}
