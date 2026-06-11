using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolsManagement.Api.Data;
using SchoolsManagement.Api.Models.School;
using SchoolsManagement.Api.Services;

namespace SchoolsManagement.Api.Controllers;

[ApiController]
[Route("api/bus-driver")]
[Authorize]
public class BusDriverController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public BusDriverController(ApplicationDbContext db) => _db = db;

    [HttpPost("location")]
    public async Task<IActionResult> UpdateLocation([FromBody] BusLocationUpdateRequest body, CancellationToken ct)
    {
        var driverId = BusTokenService.TryGetDriverId(User);
        if (driverId is null)
        {
            return Unauthorized(new { message = "جلسة غير صالحة." });
        }

        await BusAppTablesBootstrap.EnsureExistsAsync(_db, ct);
        var now = DateTimeOffset.UtcNow;

        _db.BusDriverLocations.Add(new BusDriverLocationRecord
        {
            Id = Guid.NewGuid(),
            DriverId = driverId.Value,
            Latitude = body.Latitude,
            Longitude = body.Longitude,
            SpeedKmh = body.SpeedKmh,
            Heading = body.Heading,
            RecordedAt = now
        });

        var published = await _db.BusAppLocations.FirstOrDefaultAsync(x => x.DriverId == driverId, ct);
        if (published is null)
        {
            published = new BusAppLocationRecord { DriverId = driverId.Value };
            _db.BusAppLocations.Add(published);
        }

        published.Latitude = body.Latitude;
        published.Longitude = body.Longitude;
        published.SpeedKmh = body.SpeedKmh;
        published.Heading = body.Heading;
        published.RecordedAt = now;

        await _db.SaveChangesAsync(ct);

        return Ok(new
        {
            success = true,
            recorded_at = now,
            latitude = body.Latitude,
            longitude = body.Longitude
        });
    }
}
