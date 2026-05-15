using Microsoft.AspNetCore.Mvc;
using Schools.Api.Models;
using Schools.Api.Services;

namespace Schools.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly JwtTokenIssuer _jwt;

    public AuthController(IConfiguration configuration, JwtTokenIssuer jwt)
    {
        _configuration = configuration;
        _jwt = jwt;
    }

    /// <summary>تسجيل دخول تجريبي — يطابق مسار Angular الحالي POST /api/auth/login.</summary>
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Login))
        {
            return BadRequest(new { message = "login_required" });
        }

        var demoLogin = (_configuration["DemoAuth:Login"] ?? "admin").Trim();
        var demoPassword = _configuration["DemoAuth:Password"] ?? "Admin123!";
        var login = request.Login.Trim();

        if (!string.Equals(login, demoLogin, StringComparison.Ordinal)
            || request.Password != demoPassword)
        {
            return Unauthorized(new { message = "Invalid credentials." });
        }

        var email = (_configuration["DemoAuth:Email"] ?? "admin@schools.local").Trim();
        var fullName = (_configuration["DemoAuth:FullName"] ?? "مدير تجريبي").Trim();
        var userType = (_configuration["DemoAuth:UserType"] ?? "إداري").Trim();
        var role = (_configuration["DemoAuth:Role"] ?? "Admin").Trim();
        var userId = (_configuration["DemoAuth:UserId"] ?? "00000000-0000-0000-0000-000000000001").Trim();

        var (token, expires) = _jwt.CreateDemoToken(userId, email, fullName, userType, role);

        return Ok(new AuthResponseDto
        {
            Token = token,
            ExpiresAtUtc = expires,
            Email = email,
            FullName = fullName,
            UserType = userType,
            Role = role
        });
    }
}
