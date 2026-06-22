using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SchoolsManagement.Api.Models.School;

[Table("bus_driver_locations")]
public class BusDriverLocationRecord
{
    [Key]
    public Guid Id { get; set; }

    [Column("driver_id")]
    public Guid DriverId { get; set; }

    [Column("latitude")]
    public double Latitude { get; set; }

    [Column("longitude")]
    public double Longitude { get; set; }

    [Column("speed_kmh")]
    public double? SpeedKmh { get; set; }

    [Column("heading")]
    public double? Heading { get; set; }

    [Column("recorded_at")]
    public DateTimeOffset RecordedAt { get; set; }
}

[Table("bus_app_drivers")]
public class BusAppDriverRecord
{
    [Key]
    public Guid Id { get; set; }

    [Column("school_id")]
    [MaxLength(120)]
    public string? SchoolId { get; set; }

    [Column("full_name")]
    [MaxLength(500)]
    public string FullName { get; set; } = string.Empty;

    [Column("phone_number")]
    [MaxLength(40)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Column("username")]
    [MaxLength(120)]
    public string Username { get; set; } = string.Empty;

    [Column("password")]
    [MaxLength(500)]
    public string PasswordHash { get; set; } = string.Empty;

    [Column("synced_at")]
    public DateTimeOffset SyncedAt { get; set; }
}

[Table("bus_app_students")]
public class BusAppStudentRecord
{
    [Key]
    public Guid Id { get; set; }

    [Column("driver_id")]
    public Guid DriverId { get; set; }

    [Column("school_id")]
    [MaxLength(120)]
    public string? SchoolId { get; set; }

    [Column("name")]
    [MaxLength(500)]
    public string Name { get; set; } = string.Empty;

    [Column("parent_phone")]
    [MaxLength(40)]
    public string? ParentPhone { get; set; }

    [Column("level")]
    [MaxLength(200)]
    public string Level { get; set; } = string.Empty;

    [Column("section")]
    [MaxLength(200)]
    public string Section { get; set; } = string.Empty;

    [Column("bus_site_name")]
    [MaxLength(300)]
    public string? BusSiteName { get; set; }

    [Column("bus_location_url")]
    [MaxLength(2000)]
    public string? BusLocationUrl { get; set; }

    [Column("synced_at")]
    public DateTimeOffset SyncedAt { get; set; }
}

[Table("bus_app_locations")]
public class BusAppLocationRecord
{
    [Key]
    [Column("driver_id")]
    public Guid DriverId { get; set; }

    [Column("school_id")]
    [MaxLength(120)]
    public string? SchoolId { get; set; }

    [Column("latitude")]
    public double Latitude { get; set; }

    [Column("longitude")]
    public double Longitude { get; set; }

    [Column("speed_kmh")]
    public double? SpeedKmh { get; set; }

    [Column("heading")]
    public double? Heading { get; set; }

    [Column("recorded_at")]
    public DateTimeOffset RecordedAt { get; set; }
}

public class BusLoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class BusLocationUpdateRequest
{
    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }

    [JsonPropertyName("speed_kmh")]
    public double? SpeedKmh { get; set; }

    [JsonPropertyName("heading")]
    public double? Heading { get; set; }
}

public class BusSyncIngestPayload
{
    [JsonPropertyName("school_id")]
    public string? SchoolId { get; set; }

    [JsonPropertyName("drivers")]
    public List<BusAppDriverIngestDto>? Drivers { get; set; }

    [JsonPropertyName("students")]
    public List<BusAppStudentIngestDto>? Students { get; set; }

    [JsonPropertyName("locations")]
    public List<BusAppLocationIngestDto>? Locations { get; set; }

    [JsonPropertyName("school_settings")]
    public BusSchoolSettingsIngestDto? SchoolSettings { get; set; }
}

public class BusAppDriverIngestDto
{
    public Guid Id { get; set; }

    [JsonPropertyName("full_name")]
    public string FullName { get; set; } = string.Empty;

    [JsonPropertyName("phone_number")]
    public string PhoneNumber { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class BusAppStudentIngestDto
{
    public Guid Id { get; set; }

    [JsonPropertyName("driver_id")]
    public Guid DriverId { get; set; }

    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("parent_phone")]
    public string? ParentPhone { get; set; }

    public string Level { get; set; } = string.Empty;
    public string Section { get; set; } = string.Empty;

    [JsonPropertyName("bus_site_name")]
    public string? BusSiteName { get; set; }

    [JsonPropertyName("bus_location_url")]
    public string? BusLocationUrl { get; set; }
}

public class BusAppLocationIngestDto
{
    [JsonPropertyName("driver_id")]
    public Guid DriverId { get; set; }

    public double Latitude { get; set; }
    public double Longitude { get; set; }

    [JsonPropertyName("speed_kmh")]
    public double? SpeedKmh { get; set; }

    public double? Heading { get; set; }

    [JsonPropertyName("recorded_at")]
    public DateTimeOffset RecordedAt { get; set; }
}

public class BusIngestResult
{
    public int Drivers { get; set; }
    public int Students { get; set; }
    public int Locations { get; set; }
}

[Table("bus_school_settings")]
public class BusSchoolSettingsRecord
{
    [Key]
    public int Id { get; set; } = 1;

    [Column("location_url")]
    [MaxLength(2000)]
    public string? LocationUrl { get; set; }

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }
}

public class BusSchoolSettingsIngestDto
{
    [JsonPropertyName("location_url")]
    public string? LocationUrl { get; set; }
}
