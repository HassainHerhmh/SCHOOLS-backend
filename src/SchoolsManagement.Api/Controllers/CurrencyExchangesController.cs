using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolsManagement.Api.Data;
using SchoolsManagement.Api.Models.Accounting;

namespace SchoolsManagement.Api.Controllers;

[ApiController]
[Route("api/currency-exchanges")]
[AllowAnonymous]
public class CurrencyExchangesController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public CurrencyExchangesController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CurrencyExchangeRecord>>> List(CancellationToken ct)
    {
        var list = await _db.CurrencyExchanges.AsNoTracking().OrderByDescending(x => x.Id).ToListAsync(ct);
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

        var entity = MapFromBody(body, id);
        _db.CurrencyExchanges.Add(entity);
        await _db.SaveChangesAsync(ct);
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

        ApplyBody(entity, body);
        await _db.SaveChangesAsync(ct);
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

    private static CurrencyExchangeRecord MapFromBody(CurrencyExchangeRecord body, int id)
    {
        var now = DateTimeOffset.UtcNow;
        return new CurrencyExchangeRecord
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
            CreatedBy = body.CreatedBy,
            BranchId = body.BranchId,
            CreatedAt = body.CreatedAt ?? now
        };
    }

    private static void ApplyBody(CurrencyExchangeRecord entity, CurrencyExchangeRecord body)
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
        entity.CreatedBy = body.CreatedBy;
        entity.BranchId = body.BranchId;
        entity.CreatedAt = body.CreatedAt ?? entity.CreatedAt;
    }
}
