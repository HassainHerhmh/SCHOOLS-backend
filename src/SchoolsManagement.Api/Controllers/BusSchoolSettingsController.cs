using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolsManagement.Api.Data;
using SchoolsManagement.Api.Models.School;
using SchoolsManagement.Api.Services;

namespace SchoolsManagement.Api.Controllers;

[ApiController]
[Route("api/bus-school-settings")]
[AllowAnonymous]
public class BusSchoolSettingsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly BusMapsUrlExpander _maps;

    public BusSchoolSettingsController(ApplicationDbContext db, BusMapsUrlExpander maps)
    {
        _db = db;
        _maps = maps;
    }

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
        var url = await _maps.NormalizeForStorageAsync(body.LocationUrl, ct);

        var row = await _db.BusSchoolSettings.FirstOrDefaultAsync(x => x.Id == 1, ct);
        if (row is null)
        {
            row = new BusSchoolSettingsRecord { Id = 1 };
            _db.BusSchoolSettings.Add(row);
        }

        row.LocationUrl = url;
        row.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);

        var coords = await _maps.ResolveCoordinatesAsync(row.LocationUrl, ct);

        return Ok(new
        {
            message = "تم حفظ موقع المدرسة.",
            location_url = row.LocationUrl,
            latitude = coords?.Latitude,
            longitude = coords?.Longitude,
            updated_at = row.UpdatedAt
        });
    }
}

public class SaveBusSchoolSettingsRequest
{
    public string? LocationUrl { get; set; }
}
