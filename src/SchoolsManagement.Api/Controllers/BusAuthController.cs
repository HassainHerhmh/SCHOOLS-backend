using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolsManagement.Api.Data;
using SchoolsManagement.Api.Models.School;
using SchoolsManagement.Api.Services;

namespace SchoolsManagement.Api.Controllers;

[ApiController]
[Route("api/bus-auth")]
[AllowAnonymous]
public class BusAuthController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly BusTokenService _tokenService;
    private readonly IConfiguration _configuration;

    public BusAuthController(
        ApplicationDbContext db,
        BusTokenService tokenService,
        IConfiguration configuration)
    {
        _db = db;
        _tokenService = tokenService;
        _configuration = configuration;
    }

    private string Pepper => _configuration["EmployeeAuth:Pepper"] ?? "your-secret-key-here";

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] BusLoginRequest body, CancellationToken ct)
    {
        var username = (body.Username ?? string.Empty).Trim();
        var password = body.Password ?? string.Empty;
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            return Ok(new { success = false, message = "أدخل اسم المستخدم وكلمة المرور." });
        }

        var attemptKey = $"{HttpContext.Connection.RemoteIpAddress}|{username.ToLowerInvariant()}";
        if (LoginAttemptTracker.IsLocked(attemptKey, out var wait))
        {
            var minutes = Math.Max(1, (int)Math.Ceiling((wait ?? TimeSpan.FromMinutes(15)).TotalMinutes));
            return Ok(new { success = false, message = $"تم تعطيل المحاولات مؤقتاً. حاول بعد {minutes} دقيقة." });
        }

        await BusAppTablesBootstrap.EnsureExistsAsync(_db, ct);

        var published = await _db.BusAppDrivers
            .FirstOrDefaultAsync(x => x.Username == username, ct);
        if (published is not null)
        {
            if (!EmployeePasswordHasher.Verify(password, published.PasswordHash, Pepper))
            {
                LoginAttemptTracker.RecordFailure(attemptKey);
                return Ok(new { success = false, message = "كلمة المرور غير صحيحة." });
            }

            published.PasswordHash = EmployeePasswordHasher.UpgradeIfLegacy(password, published.PasswordHash, Pepper);
            await _db.SaveChangesAsync(ct);
            LoginAttemptTracker.Clear(attemptKey);
            return Ok(BuildSuccess(published.Id, published.FullName, published.PhoneNumber, published.Username));
        }

        var local = await _db.BusPortalUsers
            .FirstOrDefaultAsync(x => x.Username == username, ct);
        if (local is null)
        {
            LoginAttemptTracker.RecordFailure(attemptKey);
            return Ok(new { success = false, message = "اسم المستخدم غير موجود." });
        }

        if (!EmployeePasswordHasher.Verify(password, local.PasswordHash, Pepper))
        {
            LoginAttemptTracker.RecordFailure(attemptKey);
            return Ok(new { success = false, message = "كلمة المرور غير صحيحة." });
        }

        local.PasswordHash = EmployeePasswordHasher.UpgradeIfLegacy(password, local.PasswordHash, Pepper);
        await _db.SaveChangesAsync(ct);
        LoginAttemptTracker.Clear(attemptKey);
        return Ok(BuildSuccess(local.Id, local.FullName, local.PhoneNumber, local.Username));
    }

    private object BuildSuccess(Guid id, string fullName, string phone, string username)
    {
        var token = _tokenService.CreateDriverToken(id, fullName, username);
        return new
        {
            success = true,
            message = "تم تسجيل الدخول بنجاح",
            token,
            driver = new
            {
                id,
                full_name = fullName,
                phone_number = phone,
                username
            }
        };
    }
}
