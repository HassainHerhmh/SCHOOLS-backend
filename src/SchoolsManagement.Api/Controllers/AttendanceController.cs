using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolsManagement.Api.Data;
using SchoolsManagement.Api.Models.School;

namespace SchoolsManagement.Api.Controllers;

[ApiController]
[Route("api/attendance")]
[AllowAnonymous]
public class AttendanceController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public AttendanceController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet("by-date")]
    public async Task<IActionResult> GetByDate(
        [FromQuery] Guid classId, 
        [FromQuery] string section, 
        [FromQuery] DateTime date,
        CancellationToken cancellationToken)
    {
        var records = await _db.AttendanceRecords
            .Where(a => a.ClassId == classId && a.Section == section && a.Date == date.Date)
            .ToListAsync(cancellationToken);
        
        return Ok(records);
    }

    [HttpGet("by-date-range")]
    public async Task<IActionResult> GetByDateRange(
        [FromQuery] Guid classId, 
        [FromQuery] string section, 
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        CancellationToken cancellationToken)
    {
        var records = await _db.AttendanceRecords
            .Where(a => a.ClassId == classId && a.Section == section && a.Date >= startDate.Date && a.Date <= endDate.Date)
            .ToListAsync(cancellationToken);
        
        return Ok(records);
    }

    [HttpPost("bulk")]
    public async Task<IActionResult> SaveBulk([FromBody] List<AttendanceRecord> records, CancellationToken cancellationToken)
    {
        if (records == null || !records.Any())
            return BadRequest("No records provided");

        var classId = records.First().ClassId;
        var section = records.First().Section;
        var date = records.First().Date.Date;

        // مسح الحضور القديم لنفس اليوم والصف والشعبة
        var existing = await _db.AttendanceRecords
            .Where(a => a.ClassId == classId && a.Section == section && a.Date == date)
            .ToListAsync(cancellationToken);
            
        _db.AttendanceRecords.RemoveRange(existing);

        // إضافة الجديد
        foreach (var rec in records)
        {
            rec.Date = rec.Date.Date; // تأكيد التخلص من الوقت
            rec.CreatedAt = DateTimeOffset.UtcNow;
            _db.AttendanceRecords.Add(rec);
        }

        await _db.SaveChangesAsync(cancellationToken);
        
        return Ok(new { message = "Attendance saved successfully" });
    }
}