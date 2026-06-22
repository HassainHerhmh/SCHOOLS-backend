using Microsoft.EntityFrameworkCore;
using SchoolsManagement.Api.Data;
using SchoolsManagement.Api.Models.School;

namespace SchoolsManagement.Api.Services;

public class BusAppIngestService
{
    private readonly ApplicationDbContext _db;
    private readonly BusMapsUrlExpander _maps;

    public BusAppIngestService(ApplicationDbContext db, BusMapsUrlExpander maps)
    {
        _db = db;
        _maps = maps;
    }

    public async Task<BusIngestResult> IngestAsync(BusSyncIngestPayload payload, CancellationToken cancellationToken = default)
    {
        await BusAppTablesBootstrap.EnsureExistsAsync(_db, cancellationToken);
        var syncedAt = DateTimeOffset.UtcNow;
        var result = new BusIngestResult();
        var schoolId = payload.SchoolId?.Trim();

        if (payload.Drivers is { Count: > 0 })
        {
            foreach (var driver in payload.Drivers)
            {
                var row = await _db.BusAppDrivers.FirstOrDefaultAsync(x => x.Id == driver.Id, cancellationToken);
                if (row is null)
                {
                    row = new BusAppDriverRecord { Id = driver.Id };
                    _db.BusAppDrivers.Add(row);
                }

                row.SchoolId = schoolId;
                row.FullName = driver.FullName;
                row.PhoneNumber = driver.PhoneNumber;
                row.Username = driver.Username;
                row.PasswordHash = driver.Password;
                row.SyncedAt = syncedAt;
                result.Drivers++;
            }

            await _db.SaveChangesAsync(cancellationToken);
        }

        if (payload.Students is { Count: > 0 })
        {
            foreach (var student in payload.Students)
            {
                var row = await _db.BusAppStudents.FirstOrDefaultAsync(x => x.Id == student.Id, cancellationToken);
                if (row is null)
                {
                    row = new BusAppStudentRecord { Id = student.Id };
                    _db.BusAppStudents.Add(row);
                }

                row.DriverId = student.DriverId;
                row.SchoolId = schoolId;
                row.Name = student.Name;
                row.ParentPhone = student.ParentPhone;
                row.Level = student.Level;
                row.Section = student.Section;
                row.BusSiteName = student.BusSiteName;
                row.BusLocationUrl = student.BusLocationUrl;
                row.SyncedAt = syncedAt;
                result.Students++;
            }

            await _db.SaveChangesAsync(cancellationToken);
        }

        if (payload.Locations is { Count: > 0 })
        {
            foreach (var location in payload.Locations)
            {
                var row = await _db.BusAppLocations.FirstOrDefaultAsync(x => x.DriverId == location.DriverId, cancellationToken);
                if (row is null)
                {
                    row = new BusAppLocationRecord { DriverId = location.DriverId };
                    _db.BusAppLocations.Add(row);
                }

                row.SchoolId = schoolId;
                row.Latitude = location.Latitude;
                row.Longitude = location.Longitude;
                row.SpeedKmh = location.SpeedKmh;
                row.Heading = location.Heading;
                row.RecordedAt = location.RecordedAt;
                result.Locations++;
            }

            await _db.SaveChangesAsync(cancellationToken);
        }

        if (payload.SchoolSettings is not null)
        {
            var row = await _db.BusSchoolSettings.FirstOrDefaultAsync(x => x.Id == 1, cancellationToken);
            if (row is null)
            {
                row = new BusSchoolSettingsRecord { Id = 1 };
                _db.BusSchoolSettings.Add(row);
            }

            row.LocationUrl = await _maps.NormalizeForStorageAsync(payload.SchoolSettings.LocationUrl, cancellationToken);
            row.UpdatedAt = syncedAt;
            await _db.SaveChangesAsync(cancellationToken);
        }

        return result;
    }
}
