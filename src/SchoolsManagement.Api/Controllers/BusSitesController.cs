using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolsManagement.Api.Data;
using SchoolsManagement.Api.Models.School;

namespace SchoolsManagement.Api.Controllers;

[ApiController]
[Route("api/bus-sites")]
[AllowAnonymous]
public class BusSitesController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public BusSitesController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BusSiteRecord>>> GetAll(CancellationToken ct)
    {
        var list = await _db.BusSites
            .AsNoTracking()
            .OrderBy(x => x.SiteName)
            .ToListAsync(ct);
        return Ok(list);
    }

    [HttpPost]
    public async Task<ActionResult<BusSiteRecord>> Create([FromBody] BusSiteUpsertRequest body, CancellationToken ct)
    {
        var name = (body.SiteName ?? "").Trim();
        if (string.IsNullOrEmpty(name))
        {
            return BadRequest(new { message = "اسم الموقع مطلوب." });
        }

        if (body.FeeAmount < 0)
        {
            return BadRequest(new { message = "قيمة الرسوم لا يمكن أن تكون سالبة." });
        }

        if (await _db.BusSites.AnyAsync(s => s.SiteName == name, ct))
        {
            return Conflict(new { message = "يوجد موقع بنفس الاسم مسبقاً." });
        }

        var now = DateTimeOffset.UtcNow;
        var entity = new BusSiteRecord
        {
            Id = Guid.NewGuid(),
            SiteName = name,
            FeeAmount = body.FeeAmount,
            CreatedAt = now
        };
        _db.BusSites.Add(entity);
        await _db.SaveChangesAsync(ct);
        return Created($"/api/bus-sites/{entity.Id}", entity);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] BusSiteUpsertRequest body, CancellationToken ct)
    {
        var entity = await _db.BusSites.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (entity is null) return NotFound(new { message = "الموقع غير موجود." });

        var name = (body.SiteName ?? "").Trim();
        if (string.IsNullOrEmpty(name))
        {
            return BadRequest(new { message = "اسم الموقع مطلوب." });
        }

        if (body.FeeAmount < 0)
        {
            return BadRequest(new { message = "قيمة الرسوم لا يمكن أن تكون سالبة." });
        }

        if (await _db.BusSites.AnyAsync(s => s.SiteName == name && s.Id != id, ct))
        {
            return Conflict(new { message = "يوجد موقع بنفس الاسم مسبقاً." });
        }

        entity.SiteName = name;
        entity.FeeAmount = body.FeeAmount;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var entity = await _db.BusSites.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (entity is null) return NotFound(new { message = "الموقع غير موجود." });

        var inUse = await _db.StudentRecords.AsNoTracking().AnyAsync(s => s.BusSiteId == id, ct);
        if (inUse)
        {
            return Conflict(new { message = "لا يمكن حذف الموقع — مرتبط بطلاب. غيّر موقع الطلاب أولاً." });
        }

        _db.BusSites.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpGet("site-name-available")]
    public async Task<ActionResult<object>> SiteNameAvailable([FromQuery] string site_name, [FromQuery] Guid? exclude_id, CancellationToken ct)
    {
        var name = (site_name ?? "").Trim();
        if (string.IsNullOrEmpty(name)) return Ok(new { taken = false });

        var exists = exclude_id.HasValue
            ? await _db.BusSites.AnyAsync(x => x.SiteName == name && x.Id != exclude_id.Value, ct)
            : await _db.BusSites.AnyAsync(x => x.SiteName == name, ct);

        return Ok(new { taken = exists });
    }
}
