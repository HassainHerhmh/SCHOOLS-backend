using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolsManagement.Api.Data;
using SchoolsManagement.Api.Models.School;

namespace SchoolsManagement.Api.Controllers;

[ApiController]
[Route("api/bus-users")]
[AllowAnonymous]
public class BusUsersController : ControllerBase
{
    private const string PasswordChars = "abcdefghjkmnpqrstuvwxyzABCDEFGHJKMNPQRSTUVWXYZ23456789";
    private readonly ApplicationDbContext _db;

    public BusUsersController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetAll(CancellationToken ct)
    {
        var rows = await _db.BusPortalUsers
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt ?? DateTimeOffset.MinValue)
            .Select(x => new
            {
                x.Id,
                x.FullName,
                x.PhoneNumber,
                x.Username
            })
            .ToListAsync(ct);
        return Ok(rows);
    }

    [HttpPost]
    public async Task<ActionResult<object>> Create([FromBody] BusUserCreateRequest body, CancellationToken ct)
    {
        var fullName = (body.FullName ?? "").Trim();
        var phone = (body.PhoneNumber ?? "").Trim();
        var username = (body.Username ?? "").Trim();
        var password = body.Password ?? "";

        if (string.IsNullOrEmpty(fullName)) return BadRequest(new { message = "الاسم مطلوب." });
        if (string.IsNullOrEmpty(phone)) return BadRequest(new { message = "رقم الهاتف مطلوب." });
        if (string.IsNullOrEmpty(username)) return BadRequest(new { message = "اسم المستخدم مطلوب." });
        if (password.Length == 0) return BadRequest(new { message = "كلمة المرور مطلوبة." });

        if (await _db.BusPortalUsers.AnyAsync(u => u.Username == username, ct))
        {
            return Conflict(new { message = "اسم المستخدم مستخدم مسبقاً." });
        }

        var now = DateTimeOffset.UtcNow;
        var entity = new BusPortalUserRecord
        {
            Id = Guid.NewGuid(),
            FullName = fullName,
            PhoneNumber = phone,
            Username = username,
            Password = password,
            CreatedAt = now
        };
        _db.BusPortalUsers.Add(entity);
        await _db.SaveChangesAsync(ct);

        return StatusCode(StatusCodes.Status201Created, new
        {
            id = entity.Id,
            full_name = entity.FullName,
            phone_number = entity.PhoneNumber,
            username = entity.Username,
            password = entity.Password
        });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] BusUserUpdateRequest body, CancellationToken ct)
    {
        var entity = await _db.BusPortalUsers.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (entity is null) return NotFound(new { message = "المستخدم غير موجود." });

        var fullName = (body.FullName ?? "").Trim();
        var phone = (body.PhoneNumber ?? "").Trim();
        var username = (body.Username ?? "").Trim();

        if (string.IsNullOrEmpty(fullName)) return BadRequest(new { message = "الاسم مطلوب." });
        if (string.IsNullOrEmpty(phone)) return BadRequest(new { message = "رقم الهاتف مطلوب." });
        if (string.IsNullOrEmpty(username)) return BadRequest(new { message = "اسم المستخدم مطلوب." });

        if (await _db.BusPortalUsers.AnyAsync(u => u.Username == username && u.Id != id, ct))
        {
            return Conflict(new { message = "اسم المستخدم مستخدم مسبقاً." });
        }

        entity.FullName = fullName;
        entity.PhoneNumber = phone;
        entity.Username = username;
        if (!string.IsNullOrEmpty(body.Password))
        {
            entity.Password = body.Password;
        }

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var entity = await _db.BusPortalUsers.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (entity is null) return NotFound(new { message = "المستخدم غير موجود." });

        _db.BusPortalUsers.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpGet("username-available")]
    public async Task<ActionResult<object>> UsernameAvailable([FromQuery] string username, [FromQuery] Guid? exclude_id, CancellationToken ct)
    {
        var u = (username ?? "").Trim();
        if (string.IsNullOrEmpty(u)) return Ok(new { taken = false });

        var exists = exclude_id.HasValue
            ? await _db.BusPortalUsers.AnyAsync(x => x.Username == u && x.Id != exclude_id.Value, ct)
            : await _db.BusPortalUsers.AnyAsync(x => x.Username == u, ct);

        return Ok(new { taken = exists });
    }

    [HttpPost("{id:guid}/reset-password")]
    public async Task<ActionResult<object>> ResetPassword(Guid id, CancellationToken ct)
    {
        var entity = await _db.BusPortalUsers.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (entity is null) return NotFound(new { message = "المستخدم غير موجود." });

        var newPass = GeneratePortalPassword();
        entity.Password = newPass;
        await _db.SaveChangesAsync(ct);

        return Ok(new { password = newPass });
    }

    private static string GeneratePortalPassword(int length = 12)
    {
        var bytes = new byte[length];
        System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
        var chars = PasswordChars.AsSpan();
        var sb = new System.Text.StringBuilder(length);
        for (var i = 0; i < length; i++)
        {
            sb.Append(chars[bytes[i] % chars.Length]);
        }

        return sb.ToString();
    }
}
