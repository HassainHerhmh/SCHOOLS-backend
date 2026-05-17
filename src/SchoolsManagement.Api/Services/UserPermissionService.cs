using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SchoolsManagement.Api.Data;
using SchoolsManagement.Api.Models.Identity;
using SchoolsManagement.Api.Security;

namespace SchoolsManagement.Api.Services;

public class UserPermissionService
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public UserPermissionService(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<bool> IsAdminAsync(ApplicationUser user, CancellationToken ct = default) =>
        await _userManager.IsInRoleAsync(user, "Admin");

    public async Task<IReadOnlyList<string>> GetEffectivePermissionsAsync(
        ApplicationUser user,
        CancellationToken ct = default)
    {
        if (await IsAdminAsync(user, ct))
        {
            return PermissionCatalog.AllKeys.ToList();
        }

        return await _db.UserPagePermissions.AsNoTracking()
            .Where(x => x.UserId == user.Id)
            .Select(x => x.PermissionKey)
            .Distinct()
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<string>> GetStoredPermissionsAsync(string userId, CancellationToken ct) =>
        await _db.UserPagePermissions.AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => x.PermissionKey)
            .Distinct()
            .ToListAsync(ct);

    public async Task SetPermissionsAsync(string userId, IEnumerable<string> keys, CancellationToken ct)
    {
        var normalized = keys
            .Select(k => k.Trim())
            .Where(PermissionCatalog.IsValidKey)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var existing = await _db.UserPagePermissions.Where(x => x.UserId == userId).ToListAsync(ct);
        _db.UserPagePermissions.RemoveRange(existing);

        foreach (var key in normalized)
        {
            _db.UserPagePermissions.Add(new UserPagePermissionRecord
            {
                UserId = userId,
                PermissionKey = key,
            });
        }

        await _db.SaveChangesAsync(ct);
    }
}
