using System.Security.Cryptography;
using System.Text;

namespace SchoolsManagement.Api.Services;

public static class EmployeePasswordHasher
{
    public static string Hash(string password, string pepper)
    {
        var p = string.IsNullOrEmpty(password) ? "123456" : password;
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(p + pepper));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
