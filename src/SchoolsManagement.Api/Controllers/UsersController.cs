using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolsManagement.Api.Models.Identity;

namespace SchoolsManagement.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private static readonly string[] AllowedRoles = { "Admin", "Teacher", "Staff" };

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public UsersController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserListItem>>> GetAll(CancellationToken ct)
    {
        var users = await _userManager.Users.OrderBy(u => u.UserName).ToListAsync(ct);
        var list = new List<UserListItem>(users.Count);
        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            var now = DateTimeOffset.UtcNow;
            var disabled = u.LockoutEnabled
                           && u.LockoutEnd.HasValue
                           && u.LockoutEnd.Value > now;

            list.Add(new UserListItem
            {
                Id = u.Id,
                UserName = u.UserName ?? "",
                Email = u.Email ?? "",
                FullName = u.FullName,
                PhoneNumber = u.PhoneNumber,
                UserType = u.UserType,
                Role = roles.FirstOrDefault() ?? "Staff",
                IsDisabled = disabled,
            });
        }

        return Ok(list);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest body, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.Role) || !AllowedRoles.Contains(body.Role))
        {
            return BadRequest(new { message = "دور غير مسموح أو غير محدد." });
        }

        if (await _userManager.FindByNameAsync(body.UserName) != null)
        {
            return Conflict(new { message = "اسم المستخدم مستخدم مسبقاً." });
        }

        if (await _userManager.FindByEmailAsync(body.Email) != null)
        {
            return Conflict(new { message = "البريد الإلكتروني مستخدم مسبقاً." });
        }

        var user = new ApplicationUser
        {
            UserName = body.UserName.Trim(),
            Email = body.Email.Trim(),
            FullName = body.FullName?.Trim(),
            PhoneNumber = string.IsNullOrWhiteSpace(body.PhoneNumber) ? null : body.PhoneNumber.Trim(),
            UserType = string.IsNullOrWhiteSpace(body.UserType) ? "إداري" : body.UserType.Trim(),
            EmailConfirmed = true,
        };

        var create = await _userManager.CreateAsync(user, body.Password);
        if (!create.Succeeded)
        {
            return BadRequest(new { message = string.Join("؛ ", create.Errors.Select(e => e.Description)) });
        }

        if (!await _roleManager.RoleExistsAsync(body.Role))
        {
            await _roleManager.CreateAsync(new IdentityRole(body.Role));
        }

        var roleResult = await _userManager.AddToRoleAsync(user, body.Role);
        if (!roleResult.Succeeded)
        {
            return BadRequest(new { message = string.Join("؛ ", roleResult.Errors.Select(e => e.Description)) });
        }

        return Ok(new { message = "تم إنشاء المستخدم.", id = user.Id });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateUserRequest body, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.Role) || !AllowedRoles.Contains(body.Role))
        {
            return BadRequest(new { message = "دور غير مسموح أو غير محدد." });
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        var emailOwner = await _userManager.FindByEmailAsync(body.Email);
        if (emailOwner != null && emailOwner.Id != id)
        {
            return Conflict(new { message = "البريد الإلكتروني مستخدم لمستخدم آخر." });
        }

        user.Email = body.Email.Trim();
        user.FullName = body.FullName?.Trim();
        user.PhoneNumber = string.IsNullOrWhiteSpace(body.PhoneNumber) ? null : body.PhoneNumber.Trim();
        user.UserType = string.IsNullOrWhiteSpace(body.UserType) ? "إداري" : body.UserType.Trim();

        var update = await _userManager.UpdateAsync(user);
        if (!update.Succeeded)
        {
            return BadRequest(new { message = string.Join("؛ ", update.Errors.Select(e => e.Description)) });
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        if (!await _roleManager.RoleExistsAsync(body.Role))
        {
            await _roleManager.CreateAsync(new IdentityRole(body.Role));
        }

        await _userManager.AddToRoleAsync(user, body.Role);

        return Ok(new { message = "تم حفظ التعديلات." });
    }

    [HttpPost("{id}/disable")]
    public async Task<IActionResult> Disable(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        await _userManager.SetLockoutEnabledAsync(user, true);
        await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(200));
        return Ok(new { message = "تم تعطيل المستخدم." });
    }

    [HttpPost("{id}/enable")]
    public async Task<IActionResult> Enable(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        await _userManager.SetLockoutEndDateAsync(user, null);
        await _userManager.SetLockoutEnabledAsync(user, false);
        return Ok(new { message = "تم تفعيل المستخدم." });
    }

    [HttpPost("{id}/reset-password")]
    public async Task<IActionResult> ResetPassword(string id, [FromBody] ResetPasswordRequest body)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var reset = await _userManager.ResetPasswordAsync(user, token, body.NewPassword);
        if (!reset.Succeeded)
        {
            return BadRequest(new { message = string.Join("؛ ", reset.Errors.Select(e => e.Description)) });
        }

        return Ok(new { message = "تم تغيير كلمة المرور." });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        var del = await _userManager.DeleteAsync(user);
        if (!del.Succeeded)
        {
            return BadRequest(new { message = string.Join("؛ ", del.Errors.Select(e => e.Description)) });
        }

        return Ok(new { message = "تم حذف المستخدم." });
    }
}
