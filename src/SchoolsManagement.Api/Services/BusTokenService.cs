using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace SchoolsManagement.Api.Services;

public class BusTokenService
{
    private readonly IConfiguration _configuration;

    public BusTokenService(IConfiguration configuration) => _configuration = configuration;

    public string CreateDriverToken(Guid driverId, string fullName, string username)
    {
        var jwtSection = _configuration.GetSection("Jwt");
        var secret = jwtSection["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey is missing.");
        var issuer = jwtSection["Issuer"] ?? throw new InvalidOperationException("JWT Issuer is missing.");
        var audience = jwtSection["Audience"] ?? throw new InvalidOperationException("JWT Audience is missing.");

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, driverId.ToString()),
            new(ClaimTypes.NameIdentifier, driverId.ToString()),
            new(ClaimTypes.Name, fullName),
            new("user_type", "bus_driver"),
            new("bus_driver_id", driverId.ToString()),
            new("username", username)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var minutes = int.TryParse(jwtSection["AccessTokenLifetimeMinutes"], out var parsed) ? parsed : 480;
        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            expires: DateTime.UtcNow.AddMinutes(minutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static Guid? TryGetDriverId(ClaimsPrincipal? user)
    {
        var raw = user?.FindFirst("bus_driver_id")?.Value
                  ?? user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
