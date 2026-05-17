using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SchoolsManagement.Api.Models.Identity;

public class CreateUserRequest
{
    [Required]
    [JsonPropertyName("user_name")]
    public string UserName { get; set; } = "";

    [Required]
    [EmailAddress]
    [JsonPropertyName("email")]
    public string Email { get; set; } = "";

    [Required]
    [MinLength(8)]
    [JsonPropertyName("password")]
    public string Password { get; set; } = "";

    [JsonPropertyName("full_name")]
    public string? FullName { get; set; }

    [JsonPropertyName("phone_number")]
    public string? PhoneNumber { get; set; }

    [JsonPropertyName("user_type")]
    public string UserType { get; set; } = "إداري";

    [Required]
    [JsonPropertyName("role")]
    public string Role { get; set; } = "Staff";
}

public class UpdateUserRequest
{
    [Required]
    [EmailAddress]
    [JsonPropertyName("email")]
    public string Email { get; set; } = "";

    [JsonPropertyName("full_name")]
    public string? FullName { get; set; }

    [JsonPropertyName("phone_number")]
    public string? PhoneNumber { get; set; }

    [JsonPropertyName("user_type")]
    public string UserType { get; set; } = "إداري";

    [Required]
    [JsonPropertyName("role")]
    public string Role { get; set; } = "Staff";
}

public class ResetPasswordRequest
{
    [Required]
    [MinLength(8)]
    [JsonPropertyName("new_password")]
    public string NewPassword { get; set; } = "";
}
