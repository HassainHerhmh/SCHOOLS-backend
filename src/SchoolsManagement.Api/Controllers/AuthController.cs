using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolsManagement.Api.Models.Auth;
using SchoolsManagement.Api.Models.Identity;
using SchoolsManagement.Api.Services;

namespace SchoolsManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ITokenService _tokenService;
    private readonly UserPermissionService _permissionService;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ITokenService tokenService,
        UserPermissionService permissionService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _permissionService = permissionService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var exists = await _userManager.FindByEmailAsync(request.Email);
        if (exists is not null)
        {
            return BadRequest(new { message = "Email already exists." });
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FullName = string.IsNullOrWhiteSpace(request.FullName) ? null : request.FullName.Trim(),
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            return BadRequest(createResult.Errors);
        }

        var roleManager = HttpContext.RequestServices.GetRequiredService<RoleManager<IdentityRole>>();
        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            await roleManager.CreateAsync(new IdentityRole("Admin"));
        }

        await _userManager.AddToRoleAsync(user, "Admin");

        return Ok(new { message = "User registered successfully." });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Login))
        {
            return BadRequest(new { message = "login_required" });
        }

        var user = await FindUserByLoginAsync(request.Login);
        if (user is null)
        {
            return Unauthorized(new { message = "Invalid credentials." });
        }

        var signInResult = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (signInResult.Succeeded)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var permissions = await _permissionService.GetEffectivePermissionsAsync(user);
            var isAdmin = await _permissionService.IsAdminAsync(user);
            var fullAccess = await _permissionService.HasUnrestrictedAccessAsync(user);
            var token = _tokenService.CreateToken(user, roles, permissions.ToList());
            var expiry = _tokenService.GetExpiryUtc();
            var displayName = ResolveDisplayName(user);
            var primaryRole = roles.FirstOrDefault() ?? string.Empty;
            var userType = string.IsNullOrWhiteSpace(user.UserType) ? "إداري" : user.UserType.Trim();

            return Ok(new AuthResponse
            {
                Token = token,
                ExpiresAtUtc = expiry,
                Email = user.Email ?? string.Empty,
                FullName = displayName,
                UserType = userType,
                Role = primaryRole,
                Permissions = permissions.ToList(),
                IsAdmin = isAdmin,
                FullAccess = fullAccess,
            });
        }

        if (signInResult.IsLockedOut)
        {
            return Unauthorized(new { message = "account_locked" });
        }

        if (signInResult.IsNotAllowed)
        {
            return Unauthorized(new { message = "not_allowed" });
        }

        return Unauthorized(new { message = "Invalid credentials." });
    }

    private async Task<ApplicationUser?> FindUserByLoginAsync(string raw)
    {
        var id = raw.Trim();
        if (string.IsNullOrEmpty(id))
        {
            return null;
        }

        var byEmail = await _userManager.FindByEmailAsync(id);
        if (byEmail is not null)
        {
            return byEmail;
        }

        var byName = await _userManager.FindByNameAsync(id);
        if (byName is not null)
        {
            return byName;
        }

        return await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == id);
    }

    /// <summary>اسم الهيدر: الاسم الكامل ثم اسم مستخدم ليس بريداً ثم الجوال ثم البريد.</summary>
    private static string ResolveDisplayName(ApplicationUser user)
    {
        if (!string.IsNullOrWhiteSpace(user.FullName))
        {
            return user.FullName.Trim();
        }

        var un = user.UserName?.Trim();
        if (!string.IsNullOrEmpty(un) && !un.Contains('@'))
        {
            return un;
        }

        if (!string.IsNullOrWhiteSpace(user.PhoneNumber))
        {
            return user.PhoneNumber.Trim();
        }

        return user.Email?.Trim() ?? string.Empty;
    }
}
