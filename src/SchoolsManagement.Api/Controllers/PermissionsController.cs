using System.Text.Json.Nodes;
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
    private static readonly string[] AllowedRoles = { "Admin", "Teacher", "Staff" };

    private readonly UserPermissionService _permissions;
    private readonly PermissionMatrixService _matrix;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public PermissionsController(
        UserPermissionService permissions,
        PermissionMatrixService matrix,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _permissions = permissions;
        _matrix = matrix;
        _userManager = userManager;
        _roleManager = roleManager;
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
        var fullAccess = await _permissions.HasUnrestrictedAccessAsync(user, ct);
        return Ok(new { permissions = list, is_admin = isAdmin, full_access = fullAccess });
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
        var fullAccess = await _permissions.HasUnrestrictedAccessAsync(user, ct);
        return Ok(new
        {
            user_id = userId,
            user_name = user.UserName,
            full_name = user.FullName,
            is_admin = isAdmin,
            full_access = fullAccess,
            stored_permissions = stored,
            effective_permissions = effective,
        });
    }

    public class SetPermissionsBody
    {
        public List<string> Permissions { get; set; } = [];
    }

    [HttpGet("users/{userId}/matrix")]
    public async Task<ActionResult<object>> GetMatrixForUser(string userId, CancellationToken ct)
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

        var roles = await _userManager.GetRolesAsync(user);
        var isAdmin = await _permissions.IsAdminAsync(user, ct);
        var fullAccess = await _permissions.HasUnrestrictedAccessAsync(user, ct);
        var stored = await _permissions.GetStoredPermissionsAsync(userId, ct);

        JsonObject matrix;
        if (fullAccess)
        {
            matrix = _matrix.CreateFullMatrix();
        }
        else
        {
            var parsed = _matrix.TryParse(user.PermissionsJson);
            matrix = _matrix.NormalizeMatrix(parsed, stored);
        }

        return Ok(new
        {
            user_id = userId,
            role = roles.FirstOrDefault() ?? "Staff",
            is_admin = isAdmin,
            full_access = fullAccess,
            permissions = _matrix.ToClientMatrix(matrix),
        });
    }

    public class SetMatrixBody
    {
        public string? Role { get; set; }
        public Dictionary<string, Dictionary<string, bool>>? Permissions { get; set; }
    }

    [HttpPut("users/{userId}/matrix")]
    public async Task<IActionResult> SetMatrixForUser(string userId, [FromBody] SetMatrixBody body, CancellationToken ct)
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

        if (!string.IsNullOrWhiteSpace(body.Role))
        {
            var role = body.Role.Trim();
            if (!AllowedRoles.Contains(role))
            {
                return BadRequest(new { message = "دور غير مسموح." });
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Count > 0)
            {
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
            }

            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new IdentityRole(role));
            }

            await _userManager.AddToRoleAsync(user, role);
        }

        var clientNode = body.Permissions is null
            ? new JsonObject()
            : JsonNode.Parse(System.Text.Json.JsonSerializer.Serialize(body.Permissions)) as JsonObject
              ?? new JsonObject();
        var internalMatrix = _matrix.FromClientMatrix(clientNode);
        user.PermissionsJson = _matrix.Serialize(internalMatrix);
        await _userManager.UpdateAsync(user);

        var pageKeys = _matrix.PageKeysFromMatrix(internalMatrix);
        await _permissions.SetPermissionsAsync(userId, pageKeys, ct);

        return Ok(new
        {
            message = "تم حفظ الصلاحيات.",
            permissions = _matrix.ToClientMatrix(internalMatrix),
            page_keys = pageKeys,
        });
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
