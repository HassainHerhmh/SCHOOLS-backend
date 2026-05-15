namespace Schools.Api.Models;

/// <summary>نفس حقول رد المشروع الكبير (snake_case عبر إعدادات JSON).</summary>
public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }

    public string Email { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string UserType { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;
}
