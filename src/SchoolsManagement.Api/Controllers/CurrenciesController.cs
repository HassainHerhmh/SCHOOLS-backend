using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolsManagement.Api.Data;
using SchoolsManagement.Api.Models.Accounting;

namespace SchoolsManagement.Api.Controllers;

[ApiController]
[Route("api/currencies")]
[AllowAnonymous]
public class CurrenciesController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public CurrenciesController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CurrencyRecord>>> List(CancellationToken cancellationToken)
    {
        var list = await _db.Currencies.AsNoTracking()
            .OrderBy(c => c.Id)
            .ToListAsync(cancellationToken);
        return Ok(list);
    }

    [HttpPost]
    public async Task<ActionResult<CurrencyRecord>> Create(
        [FromBody] UpsertCurrencyRequest body,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(body.NameAr) || string.IsNullOrWhiteSpace(body.Code))
        {
            return BadRequest(new { message = "اسم العملة ورمز ISO مطلوبان." });
        }

        var entity = ToEntity(body);
        _db.Currencies.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return StatusCode(StatusCodes.Status201Created, entity);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<CurrencyRecord>> Update(
        int id,
        [FromBody] UpsertCurrencyRequest body,
        CancellationToken cancellationToken)
    {
        var entity = await _db.Currencies.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(body.NameAr) || string.IsNullOrWhiteSpace(body.Code))
        {
            return BadRequest(new { message = "اسم العملة ورمز ISO مطلوبان." });
        }

        Apply(body, entity);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(entity);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var entity = await _db.Currencies.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        _db.Currencies.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static CurrencyRecord ToEntity(UpsertCurrencyRequest body)
    {
        var e = new CurrencyRecord();
        Apply(body, e);
        return e;
    }

    private static void Apply(UpsertCurrencyRequest body, CurrencyRecord e)
    {
        e.NameAr = body.NameAr.Trim();
        e.Code = body.Code.Trim().ToUpperInvariant();
        e.Symbol = body.Symbol?.Trim() ?? string.Empty;
        e.ExchangeRate = body.ExchangeRate;
        e.MinRate = body.MinRate;
        e.MaxRate = body.MaxRate;
        e.IsLocal = body.IsLocal;
        e.ConvertMode = body.ConvertMode == "/" ? "/" : "*";
    }
}
