using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SchoolsManagement.Api.Data;
using SchoolsManagement.Api.Models.School;

namespace SchoolsManagement.Api.Services;

public class BusRemoteSyncPublisher
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    public BusRemoteSyncPublisher(
        ApplicationDbContext db,
        IConfiguration config,
        IHttpClientFactory httpClientFactory)
    {
        _db = db;
        _config = config;
        _httpClientFactory = httpClientFactory;
    }

    public (string RemoteUrl, string SyncKey, string? SchoolId) GetRemoteSettings()
    {
        var url = _config["BusRoyal:RemoteApiUrl"]?.Trim().TrimEnd('/');
        var key = _config["BusRoyal:SyncApiKey"]?.Trim();
        var schoolId = _config["BusRoyal:SchoolId"]?.Trim();
        return (url ?? string.Empty, key ?? string.Empty, string.IsNullOrWhiteSpace(schoolId) ? null : schoolId);
    }

    public bool IsConfigured()
    {
        var (url, key, _) = GetRemoteSettings();
        return !string.IsNullOrWhiteSpace(url)
               && !string.IsNullOrWhiteSpace(key)
               && !url.Contains("YOUR_ROYAL", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<BusIngestResult> PublishAsync(CancellationToken cancellationToken = default)
    {
        var (remoteUrl, syncKey, schoolId) = GetRemoteSettings();
        if (string.IsNullOrWhiteSpace(remoteUrl) || string.IsNullOrWhiteSpace(syncKey))
        {
            throw new InvalidOperationException(
                "لم يُضبط سيرفر الباصات. أضف BusRoyal:RemoteApiUrl و BusRoyal:SyncApiKey في appsettings.Secrets.json");
        }

        var drivers = await _db.BusPortalUsers.AsNoTracking()
            .OrderBy(x => x.FullName)
            .Select(x => new BusAppDriverIngestDto
            {
                Id = x.Id,
                FullName = x.FullName,
                PhoneNumber = x.PhoneNumber,
                Username = x.Username,
                Password = x.PasswordHash
            })
            .ToListAsync(cancellationToken);

        var students = await _db.StudentRecords.AsNoTracking()
            .Where(s => s.BusDriverId != null)
            .OrderBy(s => s.Level)
            .ThenBy(s => s.Section)
            .ThenBy(s => s.Name)
            .Select(s => new BusAppStudentIngestDto
            {
                Id = s.Id,
                DriverId = s.BusDriverId!.Value,
                Name = s.Name,
                ParentPhone = s.ParentPhone,
                Level = s.Level,
                Section = s.Section,
                BusSiteName = s.BusSiteName,
                BusLocationUrl = s.BusLocationUrl
            })
            .ToListAsync(cancellationToken);

        var latestLocations = await _db.BusDriverLocations.AsNoTracking()
            .GroupBy(x => x.DriverId)
            .Select(g => g.OrderByDescending(x => x.RecordedAt).First())
            .ToListAsync(cancellationToken);

        var locations = latestLocations.Select(x => new BusAppLocationIngestDto
        {
            DriverId = x.DriverId,
            Latitude = x.Latitude,
            Longitude = x.Longitude,
            SpeedKmh = x.SpeedKmh,
            Heading = x.Heading,
            RecordedAt = x.RecordedAt
        }).ToList();

        await BusAppTablesBootstrap.EnsureExistsAsync(_db, cancellationToken);
        var schoolSettingsRow = await _db.BusSchoolSettings.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == 1, cancellationToken);
        BusSchoolSettingsIngestDto? schoolSettings = schoolSettingsRow is null
            ? null
            : new BusSchoolSettingsIngestDto
            {
                LocationUrl = schoolSettingsRow.LocationUrl
            };

        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromMinutes(5);
        var ingestUrl = $"{remoteUrl}/api/sync/ingest-bus";
        var payload = new BusSyncIngestPayload
        {
            SchoolId = schoolId,
            Drivers = drivers,
            Students = students,
            Locations = locations,
            SchoolSettings = schoolSettings
        };

        var json = JsonSerializer.Serialize(payload, JsonOptions);
        using var request = new HttpRequestMessage(HttpMethod.Post, ingestUrl);
        request.Headers.Add("X-Bus-Sync-Key", syncKey);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"فشل رفع بيانات الباصات: {(int)response.StatusCode} {body}");
        }

        return JsonSerializer.Deserialize<BusIngestResult>(body, JsonOptions) ?? new BusIngestResult
        {
            Drivers = drivers.Count,
            Students = students.Count,
            Locations = locations.Count
        };
    }
}
