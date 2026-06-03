using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SchoolsManagement.Api.Models.Identity;

namespace SchoolsManagement.Api.Services;

/// <summary>يملأ created_by_name من AspNetUsers عند غيابه.</summary>
public sealed class VoucherUserNameEnricher
{
    private readonly UserManager<ApplicationUser> _userManager;

    public VoucherUserNameEnricher(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<string?> ResolveMissingNameAsync(string? createdByUserId, string? existingName, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(existingName))
        {
            return existingName.Trim();
        }

        if (string.IsNullOrWhiteSpace(createdByUserId))
        {
            return await DefaultAdminDisplayNameAsync(ct);
        }

        var user = await _userManager.FindByIdAsync(createdByUserId);
        return user is null ? null : AccountingCurrentUserService.PickDisplayName(user);
    }

    public async Task<string?> DefaultAdminDisplayNameAsync(CancellationToken ct)
    {
        var user = await _userManager.Users
            .OrderBy(u => u.UserName)
            .FirstOrDefaultAsync(ct);
        return user is null ? null : AccountingCurrentUserService.PickDisplayName(user);
    }
}
