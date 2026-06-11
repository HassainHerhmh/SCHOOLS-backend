using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolsManagement.Api.Data;
using SchoolsManagement.Api.Models.School;
using SchoolsManagement.Api.Services;

namespace SchoolsManagement.Api.Controllers;

[ApiController]
[Route("api/bus-users")]
[AllowAnonymous]
public class BusUsersController : ControllerBase
{
    private const string PasswordChars = "abcdefghjkmnpqrstuvwxyzABCDEFGHJKMNPQRSTUVWXYZ23456789";
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _configuration;

    public BusUsersController(ApplicationDbContext db, IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
    }

    private string Pepper => _configuration["EmployeeAuth:Pepper"] ?? "your-secret-key-here";

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetAll(CancellationToken ct)
    {
        var users = await _db.BusPortalUsers
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

        var counts = await _db.StudentRecords
            .AsNoTracking()
            .Where(s => s.BusDriverId != null)
            .GroupBy(s => s.BusDriverId)
            .Select(g => new { DriverId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var countMap = counts.ToDictionary(x => x.DriverId!.Value, x => x.Count);

        var rows = users.Select(u => new
        {
            u.Id,
            u.FullName,
            u.PhoneNumber,
            u.Username,
            student_count = countMap.TryGetValue(u.Id, out var count) ? count : 0
        });

        return Ok(rows);
    }

    [HttpGet("{id:guid}/students")]
    public async Task<ActionResult<IEnumerable<object>>> GetStudents(Guid id, CancellationToken ct)
    {
        var exists = await _db.BusPortalUsers.AsNoTracking().AnyAsync(u => u.Id == id, ct);
        if (!exists)
        {
            return NotFound(new { message = "المستخدم غير موجود." });
        }

        var students = await _db.StudentRecords
            .AsNoTracking()
            .Where(s => s.BusDriverId == id)
            .OrderBy(s => s.Level)
            .ThenBy(s => s.Section)
            .ThenBy(s => s.Name)
            .Select(s => new
            {
                s.Id,
                s.Name,
                parent_phone = s.ParentPhone,
                s.Level,
                s.Section,
                bus_site_name = s.BusSiteName
            })
            .ToListAsync(ct);

        return Ok(students);
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
            PasswordHash = EmployeePasswordHasher.Hash(password, Pepper),
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
            password
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
            entity.PasswordHash = EmployeePasswordHasher.Hash(body.Password, Pepper);
        }

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var entity = await _db.BusPortalUsers.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (entity is null) return NotFound(new { message = "المستخدم غير موجود." });

        var hasStudents = await _db.StudentRecords.AnyAsync(s => s.BusDriverId == id, ct);
        if (hasStudents)
        {
            return Conflict(new { message = "لا يمكن حذف المستخدم — يوجد طلاب مرتبطون به كسائق باص." });
        }

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
        entity.PasswordHash = EmployeePasswordHasher.Hash(newPass, Pepper);
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
