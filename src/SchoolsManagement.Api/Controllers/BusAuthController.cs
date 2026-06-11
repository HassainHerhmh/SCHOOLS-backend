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

    public BusAuthController(ApplicationDbContext db, BusTokenService tokenService)
    {
        _db = db;
        _tokenService = tokenService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] BusLoginRequest body, CancellationToken ct)
    {
        var username = (body.Username ?? string.Empty).Trim();
        var password = body.Password ?? string.Empty;
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            return Ok(new { success = false, message = "أدخل اسم المستخدم وكلمة المرور." });
        }

        await BusAppTablesBootstrap.EnsureExistsAsync(_db, ct);

        var published = await _db.BusAppDrivers.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Username == username, ct);
        if (published is not null)
        {
            if (!string.Equals(published.Password, password, StringComparison.Ordinal))
            {
                return Ok(new { success = false, message = "كلمة المرور غير صحيحة." });
            }

            return Ok(BuildSuccess(published.Id, published.FullName, published.PhoneNumber, published.Username));
        }

        var local = await _db.BusPortalUsers.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Username == username, ct);
        if (local is null)
        {
            return Ok(new { success = false, message = "اسم المستخدم غير موجود." });
        }

        if (!string.Equals(local.Password, password, StringComparison.Ordinal))
        {
            return Ok(new { success = false, message = "كلمة المرور غير صحيحة." });
        }

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
