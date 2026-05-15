using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Schools.Api.Services;

/// <summary>إصدار JWT متوافق مع تنسيق المشروع الرئيسي (للتجربة على Railway).</summary>
public class JwtTokenIssuer
{
    private readonly IConfiguration _configuration;

    public JwtTokenIssuer(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public (string Token, DateTime ExpiresAtUtc) CreateDemoToken(
        string userId,
        string email,
        string fullName,
        string userType,
        string role)
    {
        var jwtSection = _configuration.GetSection("Jwt");
        var secret = jwtSection["SecretKey"]
            ?? throw new InvalidOperationException("Jwt:SecretKey is missing.");
        var issuer = jwtSection["Issuer"] ?? "Schools.Api";
        var audience = jwtSection["Audience"] ?? "SchoolsManagement.Client";

        var minutesText = jwtSection["AccessTokenLifetimeMinutes"];
        var minutes = int.TryParse(minutesText, out var parsed) ? parsed : 120;
        var expiryUtc = DateTime.UtcNow.AddMinutes(minutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.Email, email),
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Role, string.IsNullOrWhiteSpace(role) ? "Admin" : role)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            expires: expiryUtc,
            signingCredentials: creds);

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);
        return (jwt, expiryUtc);
    }
}
