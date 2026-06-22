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
                bus_site_name = s.BusSiteName,
                bus_location_url = s.BusLocationUrl
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
                bus_site_name = s.BusSiteName,
                bus_location_url = s.BusLocationUrl
            })
            .ToListAsync(ct);

        return Ok(local);
    }

    [HttpGet("school-location")]
    [Authorize]
    public async Task<IActionResult> SchoolLocation(CancellationToken ct)
    {
        await BusAppTablesBootstrap.EnsureExistsAsync(_db, ct);
        var row = await _db.BusSchoolSettings.AsNoTracking().FirstOrDefaultAsync(x => x.Id == 1, ct);
        var url = BusMapsUrlHelper.NormalizeUrl(row?.LocationUrl);
        double? latitude = null;
        double? longitude = null;
        if (url is not null && BusMapsUrlHelper.TryParseCoordinates(url, out var parsedLat, out var parsedLng))
        {
            latitude = parsedLat;
            longitude = parsedLng;
        }

        return Ok(new
        {
            location_url = url,
            navigation_url = url is null ? null : BusMapsUrlHelper.ToNavigationUrl(url),
            has_location = !string.IsNullOrWhiteSpace(url),
            latitude,
            longitude
        });
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

        var schoolCoords = await ResolveSchoolCoordinatesAsync(ct);
        double schoolLat;
        double schoolLng;
        if (schoolCoords.Latitude is not null && schoolCoords.Longitude is not null)
        {
            schoolLat = schoolCoords.Latitude.Value;
            schoolLng = schoolCoords.Longitude.Value;
        }
        else
        {
            var center = await ResolveRouteCenterAsync(driverId.Value, ct);
            schoolLat = center.Latitude;
            schoolLng = center.Longitude;
        }

        var points = new List<object>
        {
            new { label = "المدرسة", latitude = schoolLat, longitude = schoolLng, order = 0 }
        };

        for (var i = 0; i < sites.Count; i++)
        {
            var angle = (Math.PI * 2 * i) / Math.Max(1, sites.Count);
            points.Add(new
            {
                label = sites[i],
                latitude = schoolLat + Math.Sin(angle) * 0.02,
                longitude = schoolLng + Math.Cos(angle) * 0.02,
                order = i + 1
            });
        }

        points.Add(new { label = "العودة للمدرسة", latitude = schoolLat, longitude = schoolLng, order = sites.Count + 1 });

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

    private async Task<(double? Latitude, double? Longitude)> ResolveSchoolCoordinatesAsync(CancellationToken ct)
    {
        var row = await _db.BusSchoolSettings.AsNoTracking().FirstOrDefaultAsync(x => x.Id == 1, ct);
        if (row?.LocationUrl is not null
            && BusMapsUrlHelper.TryParseCoordinates(row.LocationUrl, out var lat, out var lng))
        {
            return (lat, lng);
        }

        return (null, null);
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
