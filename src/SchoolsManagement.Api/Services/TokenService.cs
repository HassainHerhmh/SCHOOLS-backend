using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SchoolsManagement.Api.Models.Identity;

namespace SchoolsManagement.Api.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string CreateToken(ApplicationUser user, IList<string> roles, IList<string> permissions)
    {
        var jwtSection = _configuration.GetSection("Jwt");
        var secret = jwtSection["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey is missing.");
        var issuer = jwtSection["Issuer"] ?? throw new InvalidOperationException("JWT Issuer is missing.");
        var audience = jwtSection["Audience"] ?? throw new InvalidOperationException("JWT Audience is missing.");

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email ?? string.Empty)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        claims.AddRange(permissions.Select(p => new Claim("permission", p)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiryUtc = GetExpiryUtc();

        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            expires: expiryUtc,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public DateTime GetExpiryUtc()
    {
        var minutesText = _configuration["Jwt:AccessTokenLifetimeMinutes"];
        var minutes = int.TryParse(minutesText, out var parsed) ? parsed : 120;
        return DateTime.UtcNow.AddMinutes(minutes);
    }
}
