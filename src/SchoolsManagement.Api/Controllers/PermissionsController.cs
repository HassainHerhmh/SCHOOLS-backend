using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolsManagement.Api.Models.Identity;
using SchoolsManagement.Api.Security;
using SchoolsManagement.Api.Services;
using System.Security.Claims;

namespace SchoolsManagement.Api.Controllers;

[ApiController]
[Route("api/permissions")]
[Authorize]
public class PermissionsController : ControllerBase
{
    private readonly UserPermissionService _permissions;
    private readonly UserManager<ApplicationUser> _userManager;

    public PermissionsController(UserPermissionService permissions, UserManager<ApplicationUser> userManager)
    {
        _permissions = permissions;
        _userManager = userManager;
    }

    [HttpGet("catalog")]
    public ActionResult<object> Catalog() =>
        Ok(PermissionCatalog.All.Select(x => new
        {
            key = x.Key,
            label = x.Label,
            group = x.Group,
        }));

    [HttpGet("me")]
    public async Task<ActionResult<object>> MyPermissions(CancellationToken ct)
    {
        var user = await GetCurrentUserAsync(ct);
        if (user is null)
        {
            return Unauthorized();
        }

        var list = await _permissions.GetEffectivePermissionsAsync(user, ct);
        var isAdmin = await _permissions.IsAdminAsync(user, ct);
        return Ok(new { permissions = list, is_admin = isAdmin });
    }

    [HttpGet("users/{userId}")]
    public async Task<ActionResult<object>> GetForUser(string userId, CancellationToken ct)
    {
        if (!await CanManagePermissionsAsync(ct))
        {
            return Forbid();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return NotFound();
        }

        var effective = await _permissions.GetEffectivePermissionsAsync(user, ct);
        var stored = await _permissions.GetStoredPermissionsAsync(userId, ct);
        var isAdmin = await _permissions.IsAdminAsync(user, ct);
        return Ok(new
        {
            user_id = userId,
            user_name = user.UserName,
            full_name = user.FullName,
            is_admin = isAdmin,
            stored_permissions = stored,
            effective_permissions = effective,
        });
    }

    public class SetPermissionsBody
    {
        public List<string> Permissions { get; set; } = [];
    }

    [HttpPut("users/{userId}")]
    public async Task<IActionResult> SetForUser(string userId, [FromBody] SetPermissionsBody body, CancellationToken ct)
    {
        if (!await CanManagePermissionsAsync(ct))
        {
            return Forbid();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return NotFound();
        }

        if (await _permissions.IsAdminAsync(user, ct))
        {
            return BadRequest(new { message = "مستخدم Admin يملك كل الصلاحيات تلقائياً ولا يُعدَّل من هنا." });
        }

        await _permissions.SetPermissionsAsync(userId, body.Permissions ?? [], ct);
        var effective = await _permissions.GetEffectivePermissionsAsync(user, ct);
        return Ok(new { message = "تم حفظ الصلاحيات.", permissions = effective });
    }

    private async Task<bool> CanManagePermissionsAsync(CancellationToken ct)
    {
        var user = await GetCurrentUserAsync(ct);
        if (user is null)
        {
            return false;
        }

        if (await _permissions.IsAdminAsync(user, ct))
        {
            return true;
        }

        var perms = await _permissions.GetEffectivePermissionsAsync(user, ct);
        return perms.Contains(PermissionCatalog.Permissions, StringComparer.OrdinalIgnoreCase);
    }

    private async Task<ApplicationUser?> GetCurrentUserAsync(CancellationToken ct)
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(id))
        {
            return null;
        }

        return await _userManager.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
    }
}
