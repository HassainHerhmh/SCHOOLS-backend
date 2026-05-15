namespace Schools.Api.Models;

/// <summary>نفس شكل الطلب الذي يرسله Angular (login + password).</summary>
public class LoginRequest
{
    public string Login { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}
