using System.Text.Json.Serialization;

namespace SchoolsManagement.Api.Models.School;

public class BusUserCreateRequest
{
    [JsonPropertyName("full_name")]
    public string FullName { get; set; } = string.Empty;

    [JsonPropertyName("phone_number")]
    public string PhoneNumber { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class BusUserUpdateRequest
{
    [JsonPropertyName("full_name")]
    public string FullName { get; set; } = string.Empty;

    [JsonPropertyName("phone_number")]
    public string PhoneNumber { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;
    /// <summary>إن وُجدت وغير فارغة تُستبدل كلمة المرور.</summary>
    public string? Password { get; set; }
}

public class BusSiteUpsertRequest
{
    [JsonPropertyName("site_name")]
    public string SiteName { get; set; } = string.Empty;

    [JsonPropertyName("fee_amount")]
    public decimal FeeAmount { get; set; }
}
