using SchoolsManagement.Api.Models.Identity;

namespace SchoolsManagement.Api.Services;

public interface ITokenService
{
    string CreateToken(ApplicationUser user, IList<string> roles, IList<string> permissions);
    DateTime GetExpiryUtc();
}
