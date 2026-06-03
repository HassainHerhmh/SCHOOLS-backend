using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolsManagement.Api.Data;
using SchoolsManagement.Api.Models.Accounting;
using SchoolsManagement.Api.Services;

namespace SchoolsManagement.Api.Controllers;

[ApiController]
[Route("api/currency-exchanges")]
[Authorize]
public class CurrencyExchangesController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly AccountingCurrentUserService _currentUser;
    private readonly VoucherUserNameEnricher _userNameEnricher;

    public CurrencyExchangesController(
        ApplicationDbContext db,
        AccountingCurrentUserService currentUser,
        VoucherUserNameEnricher userNameEnricher)
    {
        _db = db;
        _currentUser = currentUser;
        _userNameEnricher = userNameEnricher;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CurrencyExchangeRecord>>> List(CancellationToken ct)
    {
        var list = await _db.CurrencyExchanges.AsNoTracking().OrderByDescending(x => x.Id).ToListAsync(ct);
        await AccountingVoucherAuditHelper.EnrichDisplayNamesAsync(list, _userNameEnricher, ct);
        return Ok(list);
    }

    [HttpGet("next-id")]
    public async Task<ActionResult<object>> NextId(CancellationToken ct)
    {
        var max = await _db.CurrencyExchanges.Select(x => (int?)x.Id).MaxAsync(ct) ?? 0;
        return Ok(new { next_id = max + 1 });
    }

    [HttpPost]
    public async Task<ActionResult<CurrencyExchangeRecord>> Create([FromBody] CurrencyExchangeRecord body, CancellationToken ct)
    {
        var id = body.Id <= 0 ? await NextIdInt(ct) : body.Id;
        if (await _db.CurrencyExchanges.AnyAsync(x => x.Id == id, ct))
        {
            return Conflict(new { message = "معرّف العملية مستخدم مسبقاً." });
        }

        var entity = await MapFromBodyAsync(body, id, ct);
        _db.CurrencyExchanges.Add(entity);
        await _db.SaveChangesAsync(ct);
        await AccountingVoucherAuditHelper.EnrichDisplayNamesAsync([entity], _userNameEnricher, ct);
        return CreatedAtAction(nameof(List), entity);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<CurrencyExchangeRecord>> Update(int id, [FromBody] CurrencyExchangeRecord body, CancellationToken ct)
    {
        var entity = await _db.CurrencyExchanges.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null)
        {
            return NotFound();
        }

        await ApplyBodyAsync(entity, body, ct);
        await _db.SaveChangesAsync(ct);
        await AccountingVoucherAuditHelper.EnrichDisplayNamesAsync([entity], _userNameEnricher, ct);
        return Ok(entity);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await _db.CurrencyExchanges.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null)
        {
            return NotFound();
        }

        _db.CurrencyExchanges.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    private async Task<int> NextIdInt(CancellationToken ct)
    {
        var max = await _db.CurrencyExchanges.Select(x => (int?)x.Id).MaxAsync(ct) ?? 0;
        return max + 1;
    }

    private async Task<CurrencyExchangeRecord> MapFromBodyAsync(CurrencyExchangeRecord body, int id, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var entity = new CurrencyExchangeRecord
        {
            Id = id,
            Reference = body.Reference?.Trim() ?? "",
            ExchangeDate = body.ExchangeDate == default ? now : body.ExchangeDate,
            ExchangeType = body.ExchangeType ?? "",
            FromCurrencyId = body.FromCurrencyId,
            FromAmount = body.FromAmount,
            FromRate = body.FromRate,
            FromAccountId = body.FromAccountId,
            ToCurrencyId = body.ToCurrencyId,
            ToAmount = body.ToAmount,
            ToRate = body.ToRate,
            ToAccountId = body.ToAccountId,
            CustomerName = body.CustomerName?.Trim() ?? "",
            Notes = body.Notes ?? "",
            BranchId = body.BranchId,
            CreatedAt = body.CreatedAt ?? now
        };
        await AccountingVoucherAuditHelper.ApplyOnCreateAsync(entity, body, _currentUser, ct);
        return entity;
    }

    private async Task ApplyBodyAsync(CurrencyExchangeRecord entity, CurrencyExchangeRecord body, CancellationToken ct)
    {
        entity.Reference = body.Reference?.Trim() ?? entity.Reference;
        entity.ExchangeDate = body.ExchangeDate == default ? entity.ExchangeDate : body.ExchangeDate;
        entity.ExchangeType = body.ExchangeType ?? entity.ExchangeType;
        entity.FromCurrencyId = body.FromCurrencyId;
        entity.FromAmount = body.FromAmount;
        entity.FromRate = body.FromRate;
        entity.FromAccountId = body.FromAccountId;
        entity.ToCurrencyId = body.ToCurrencyId;
        entity.ToAmount = body.ToAmount;
        entity.ToRate = body.ToRate;
        entity.ToAccountId = body.ToAccountId;
        entity.CustomerName = body.CustomerName?.Trim() ?? "";
        entity.Notes = body.Notes ?? "";
        entity.BranchId = body.BranchId;
        entity.CreatedAt = body.CreatedAt ?? entity.CreatedAt;
        await AccountingVoucherAuditHelper.ApplyOnUpdateAsync(entity, body, _currentUser, ct);
    }
}
