using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using SchoolsManagement.Api.Models.Identity;

namespace SchoolsManagement.Api.Services;

/// <summary>اسم المستخدم الحالي لسندات القبض/الصرف والقيود ومصارفة العملة.</summary>
public sealed class AccountingCurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserManager<ApplicationUser> _userManager;

    public AccountingCurrentUserService(
        IHttpContextAccessor httpContextAccessor,
        UserManager<ApplicationUser> userManager)
    {
        _httpContextAccessor = httpContextAccessor;
        _userManager = userManager;
    }

    public async Task<string?> ResolveDisplayNameAsync(CancellationToken ct = default)
    {
        var user = await ResolveUserAsync(ct);
        if (user is null)
        {
            return null;
        }

        return PickDisplayName(user);
    }

    public async Task<string?> ResolveUserIdAsync(CancellationToken ct = default)
    {
        var user = await ResolveUserAsync(ct);
        return user?.Id;
    }

    public async Task<ApplicationUser?> ResolveUserAsync(CancellationToken ct = default)
    {
        var http = _httpContextAccessor.HttpContext;
        if (http?.User?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var id = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrWhiteSpace(id))
        {
            return await _userManager.FindByIdAsync(id);
        }

        var login = http.User.FindFirstValue(ClaimTypes.Name)
            ?? http.User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(login))
        {
            return null;
        }

        return await _userManager.FindByNameAsync(login)
            ?? await _userManager.FindByEmailAsync(login);
    }

    public static string PickDisplayName(ApplicationUser user)
    {
        if (!string.IsNullOrWhiteSpace(user.FullName))
        {
            return user.FullName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(user.UserName) && !user.UserName.Contains('@', StringComparison.Ordinal))
        {
            return user.UserName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(user.PhoneNumber))
        {
            return user.PhoneNumber.Trim();
        }

        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            return user.Email.Trim();
        }

        return user.UserName?.Trim() ?? "مستخدم";
    }
}
