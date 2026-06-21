using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolsManagement.Api.Data;
using SchoolsManagement.Api.Services;

namespace SchoolsManagement.Api.Controllers;

/// <summary>قراءة بيانات منشورة لتطبيق الباصات (سيرفر رويال أو محلي).</summary>
[ApiController]
[Route("api/bus-app")]
[AllowAnonymous]
public class BusAppController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public BusAppController(ApplicationDbContext db) => _db = db;

    [HttpGet("students")]
    [Authorize]
    public async Task<IActionResult> Students(CancellationToken ct)
    {
        var driverId = BusTokenService.TryGetDriverId(User);
        if (driverId is null)
        {
            return Unauthorized(new { message = "جلسة غير صالحة." });
        }

        await BusAppTablesBootstrap.EnsureExistsAsync(_db, ct);
        var published = await _db.BusAppStudents.AsNoTracking()
            .Where(s => s.DriverId == driverId)
            .OrderBy(s => s.Level)
            .ThenBy(s => s.Section)
            .ThenBy(s => s.Name)
            .Select(s => new
            {
                s.Id,
                s.Name,
                parent_phone = s.ParentPhone,
                s.Level,
                s.Section,
                bus_site_name = s.BusSiteName
            })
            .ToListAsync(ct);

        if (published.Count > 0)
        {
            return Ok(published);
        }

        var local = await _db.StudentRecords.AsNoTracking()
            .Where(s => s.BusDriverId == driverId)
            .OrderBy(s => s.Level)
            .ThenBy(s => s.Section)
            .ThenBy(s => s.Name)
            .Select(s => new
            {
                s.Id,
                s.Name,
                parent_phone = s.ParentPhone,
                s.Level,
                s.Section,
                bus_site_name = s.BusSiteName
            })
            .ToListAsync(ct);

        return Ok(local);
    }

    [HttpGet("location")]
    [Authorize]
    public async Task<IActionResult> Location(CancellationToken ct)
    {
        var driverId = BusTokenService.TryGetDriverId(User);
        if (driverId is null)
        {
            return Unauthorized(new { message = "جلسة غير صالحة." });
        }

        await BusAppTablesBootstrap.EnsureExistsAsync(_db, ct);

        var published = await _db.BusAppLocations.AsNoTracking()
            .FirstOrDefaultAsync(x => x.DriverId == driverId, ct);
        if (published is not null)
        {
            return Ok(MapLocation(published.Latitude, published.Longitude, published.SpeedKmh, published.Heading, published.RecordedAt));
        }

        var latest = await _db.BusDriverLocations.AsNoTracking()
            .Where(x => x.DriverId == driverId)
            .OrderByDescending(x => x.RecordedAt)
            .FirstOrDefaultAsync(ct);

        if (latest is null)
        {
            return Ok(new
            {
                latitude = 15.3694,
                longitude = 44.1910,
                speed_kmh = 0,
                heading = 0,
                recorded_at = DateTimeOffset.UtcNow,
                has_location = false
            });
        }

        return Ok(MapLocation(latest.Latitude, latest.Longitude, latest.SpeedKmh, latest.Heading, latest.RecordedAt, true));
    }

    [HttpGet("route")]
    [Authorize]
    public async Task<IActionResult> Route(CancellationToken ct)
    {
        var driverId = BusTokenService.TryGetDriverId(User);
        if (driverId is null)
        {
            return Unauthorized(new { message = "جلسة غير صالحة." });
        }

        await BusAppTablesBootstrap.EnsureExistsAsync(_db, ct);

        var sites = await _db.BusAppStudents.AsNoTracking()
            .Where(s => s.DriverId == driverId && s.BusSiteName != null && s.BusSiteName != "")
            .Select(s => s.BusSiteName!)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(ct);

        if (sites.Count == 0)
        {
            sites = await _db.StudentRecords.AsNoTracking()
                .Where(s => s.BusDriverId == driverId && s.BusSiteName != null && s.BusSiteName != "")
                .Select(s => s.BusSiteName!)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync(ct);
        }

        var (baseLat, baseLng) = await ResolveRouteCenterAsync(driverId.Value, ct);
        var points = new List<object>
        {
            new { label = "انطلاق المدرسة", latitude = baseLat, longitude = baseLng, order = 0 }
        };

        for (var i = 0; i < sites.Count; i++)
        {
            var angle = (Math.PI * 2 * i) / Math.Max(1, sites.Count);
            points.Add(new
            {
                label = sites[i],
                latitude = baseLat + Math.Sin(angle) * 0.02,
                longitude = baseLng + Math.Cos(angle) * 0.02,
                order = i + 1
            });
        }

        points.Add(new { label = "العودة للمدرسة", latitude = baseLat, longitude = baseLng, order = sites.Count + 1 });

        return Ok(new { points });
    }

    private async Task<(double Latitude, double Longitude)> ResolveRouteCenterAsync(Guid driverId, CancellationToken ct)
    {
        var published = await _db.BusAppLocations.AsNoTracking()
            .FirstOrDefaultAsync(x => x.DriverId == driverId, ct);
        if (published is not null)
        {
            return (published.Latitude, published.Longitude);
        }

        var latest = await _db.BusDriverLocations.AsNoTracking()
            .Where(x => x.DriverId == driverId)
            .OrderByDescending(x => x.RecordedAt)
            .FirstOrDefaultAsync(ct);
        if (latest is not null)
        {
            return (latest.Latitude, latest.Longitude);
        }

        return (15.3694, 44.1910);
    }

    private static object MapLocation(
        double latitude,
        double longitude,
        double? speedKmh,
        double? heading,
        DateTimeOffset recordedAt,
        bool hasLocation = true) =>
        new
        {
            latitude,
            longitude,
            speed_kmh = speedKmh ?? 0,
            heading = heading ?? 0,
            recorded_at = recordedAt,
            has_location = hasLocation
        };
}
