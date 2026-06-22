using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolsManagement.Api.Data;
using SchoolsManagement.Api.Models.School;

namespace SchoolsManagement.Api.Controllers;

[ApiController]
[Route("api/bus-school-settings")]
[AllowAnonymous]
public class BusSchoolSettingsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public BusSchoolSettingsController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        await BusAppTablesBootstrap.EnsureExistsAsync(_db, ct);
        var row = await _db.BusSchoolSettings.AsNoTracking().FirstOrDefaultAsync(x => x.Id == 1, ct);
        return Ok(new
        {
            location_url = row?.LocationUrl,
            updated_at = row?.UpdatedAt
        });
    }

    [HttpPut]
    public async Task<IActionResult> Save([FromBody] SaveBusSchoolSettingsRequest body, CancellationToken ct)
    {
        await BusAppTablesBootstrap.EnsureExistsAsync(_db, ct);
        var url = string.IsNullOrWhiteSpace(body.LocationUrl) ? null : body.LocationUrl.Trim();

        var row = await _db.BusSchoolSettings.FirstOrDefaultAsync(x => x.Id == 1, ct);
        if (row is null)
        {
            row = new BusSchoolSettingsRecord { Id = 1 };
            _db.BusSchoolSettings.Add(row);
        }

        row.LocationUrl = url;
        row.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);

        return Ok(new
        {
            message = "تم حفظ موقع المدرسة.",
            location_url = row.LocationUrl,
            updated_at = row.UpdatedAt
        });
    }
}

public class SaveBusSchoolSettingsRequest
{
    public string? LocationUrl { get; set; }
}
