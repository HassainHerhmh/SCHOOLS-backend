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

    public static bool IsStoredHash(string? value)
    {
        if (string.IsNullOrEmpty(value) || value.Length != 64)
        {
            return false;
        }

        foreach (var c in value)
        {
            if (!Uri.IsHexDigit(c))
            {
                return false;
            }
        }

        return true;
    }

    public static bool Verify(string password, string stored, string pepper)
    {
        if (IsStoredHash(stored))
        {
            return string.Equals(Hash(password, pepper), stored, StringComparison.OrdinalIgnoreCase);
        }

        return string.Equals(stored, password, StringComparison.Ordinal);
    }

    public static string UpgradeIfLegacy(string password, string stored, string pepper)
    {
        return IsStoredHash(stored) ? stored : Hash(password, pepper);
    }
}
