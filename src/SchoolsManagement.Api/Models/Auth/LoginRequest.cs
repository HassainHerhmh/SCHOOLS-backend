namespace SchoolsManagement.Api.Models.Auth;

public class LoginRequest
{
    /// <summary>البريد الإلكتروني أو اسم المستخدم (UserName).</summary>
    public string Login { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}
