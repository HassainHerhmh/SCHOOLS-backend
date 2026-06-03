using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolsManagement.Api.Data;
using SchoolsManagement.Api.Models.School;
using SchoolsManagement.Api.Services;

namespace SchoolsManagement.Api.Controllers;

[ApiController]
[Route("api/class-schedules")]
[AllowAnonymous]
public class ClassSchedulesController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ErrorLestLogger _errorLog;

    public ClassSchedulesController(ApplicationDbContext db, ErrorLestLogger errorLog)
    {
        _db = db;
        _errorLog = errorLog;
    }

    [HttpGet("settings")]
    public async Task<ActionResult<object>> GetSettings(CancellationToken ct)
    {
        try
        {
            var row = await _db.ClassScheduleSettings.AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == 1, ct);
            if (row is null)
            {
                return Ok(new { day_name = "الأحد", periods_count = 6 });
            }

            return Ok(new { day_name = row.DayName, periods_count = row.PeriodsCount });
        }
        catch (Exception ex)
        {
            _errorLog.Log(ex, HttpContext);
            return StatusCode(500, new { message = "تعذر تحميل إعدادات جدول الحصص." });
        }
    }

    [HttpPut("settings")]
    public async Task<ActionResult<object>> SaveSettings([FromBody] ScheduleSettingsRequest? body, CancellationToken ct)
    {
        try
        {
            body ??= new ScheduleSettingsRequest();
            var dayName = (body.DayName ?? body.DayNameAlt ?? string.Empty).Trim();
            var periods = body.PeriodsCount > 0 ? body.PeriodsCount : body.PeriodsCountAlt;

            if (string.IsNullOrWhiteSpace(dayName))
            {
                _errorLog.LogApiError("إعدادات جدول الحصص: اسم اليوم فارغ", HttpContext);
                return BadRequest(new { message = "اسم اليوم مطلوب." });
            }

            if (periods < 1 || periods > 12)
            {
                _errorLog.LogApiError($"إعدادات جدول الحصص: عدد حصص غير صالح ({periods})", HttpContext);
                return BadRequest(new { message = "عدد الحصص يجب أن يكون بين 1 و 12." });
            }

            var now = DateTimeOffset.UtcNow;
            var row = await _db.ClassScheduleSettings.FirstOrDefaultAsync(s => s.Id == 1, ct);
            if (row is null)
            {
                row = new ClassScheduleSettingsRecord
                {
                    Id = 1,
                    DayName = dayName,
                    PeriodsCount = periods,
                    UpdatedAt = now
                };
                _db.ClassScheduleSettings.Add(row);
            }
            else
            {
                row.DayName = dayName;
                row.PeriodsCount = periods;
                row.UpdatedAt = now;
            }

            await _db.SaveChangesAsync(ct);
            return Ok(new { day_name = row.DayName, periods_count = row.PeriodsCount });
        }
        catch (Exception ex)
        {
            _errorLog.Log(ex, HttpContext);
            return StatusCode(500, new { message = "تعذر حفظ إعدادات جدول الحصص." });
        }
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> List(
        [FromQuery] Guid? classId,
        [FromQuery] Guid? class_id,
        [FromQuery] Guid? sectionId,
        [FromQuery] Guid? section_id,
        [FromQuery] string? dayName,
        [FromQuery] string? day_name,
        CancellationToken ct)
    {
        try
        {
            var cid = classId ?? class_id;
            var sid = sectionId ?? section_id;
            var day = (dayName ?? day_name ?? string.Empty).Trim();

            var q = _db.ClassSchedulePeriods.AsNoTracking();
            if (cid.HasValue)
            {
                q = q.Where(p => p.ClassId == cid.Value);
            }

            if (sid.HasValue)
            {
                q = q.Where(p => p.SectionId == sid.Value);
            }

            if (!string.IsNullOrEmpty(day))
            {
                q = q.Where(p => p.DayName == day);
            }

            var rows = await q
                .OrderBy(p => p.ClassId)
                .ThenBy(p => p.SectionId)
                .ThenBy(p => p.DayName)
                .ThenBy(p => p.PeriodNumber)
                .ToListAsync(ct);

            if (rows.Count == 0)
            {
                return Ok(Array.Empty<object>());
            }

            var sectionIds = rows.Select(r => r.SectionId).Distinct().ToList();
            var subjectIds = rows.Where(r => r.SubjectId.HasValue).Select(r => r.SubjectId!.Value).Distinct().ToList();

            var sectionNames = await _db.SchoolSections.AsNoTracking()
                .Where(s => sectionIds.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id, s => s.Name, ct);

            var subjectNames = subjectIds.Count == 0
                ? new Dictionary<Guid, string>()
                : await _db.Subjects.AsNoTracking()
                    .Where(s => subjectIds.Contains(s.Id))
                    .ToDictionaryAsync(s => s.Id, s => s.Name, ct);

            return Ok(rows.Select(p => new
            {
                id = p.Id,
                class_id = p.ClassId,
                section_id = p.SectionId,
                section_name = sectionNames.GetValueOrDefault(p.SectionId),
                day_name = p.DayName,
                period_number = p.PeriodNumber,
                subject_id = p.SubjectId,
                subject_name = p.SubjectId.HasValue ? subjectNames.GetValueOrDefault(p.SubjectId.Value) : null,
                duration_minutes = p.DurationMinutes
            }));
        }
        catch (Exception ex)
        {
            _errorLog.Log(ex, HttpContext);
            return StatusCode(500, new { message = "تعذر تحميل جدول الحصص." });
        }
    }

    [HttpPost("bulk")]
    public async Task<ActionResult<object>> SaveBulk([FromBody] BulkScheduleRequest? body, CancellationToken ct)
    {
        try
        {
            if (body is null)
            {
                _errorLog.LogApiError("جدول الحصص: جسم الطلب فارغ", HttpContext);
                return BadRequest(new { message = "بيانات الجدول مطلوبة." });
            }

            var classId = body.ClassId != Guid.Empty ? body.ClassId : body.ClassIdAlt;
            if (classId == Guid.Empty)
            {
                _errorLog.LogApiError("جدول الحصص: الصف فارغ", HttpContext);
                return BadRequest(new { message = "الصف مطلوب." });
            }

            var day = (body.DayName ?? body.DayNameAlt ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(day))
            {
                _errorLog.LogApiError("جدول الحصص: اليوم فارغ", HttpContext);
                return BadRequest(new { message = "اليوم مطلوب." });
            }

            var applyAll = body.ApplyToAllSections || body.ApplyToAllSectionsAlt;
            var sectionIds = new List<Guid>();

            if (applyAll)
            {
                sectionIds = await _db.SchoolSections.AsNoTracking()
                    .Where(s => s.ClassId == classId)
                    .Select(s => s.Id)
                    .ToListAsync(ct);
                if (sectionIds.Count == 0)
                {
                    _errorLog.LogApiError("جدول الحصص: لا شعب للصف", HttpContext, classId.ToString());
                    return BadRequest(new { message = "لا توجد شعب في هذا الصف." });
                }
            }
            else
            {
                var sid = body.SectionId != Guid.Empty
                    ? body.SectionId
                    : (body.SectionIdAlt ?? Guid.Empty);
                if (sid == Guid.Empty)
                {
                    _errorLog.LogApiError("جدول الحصص: الشعبة فارغة", HttpContext);
                    return BadRequest(new { message = "الشعبة مطلوبة." });
                }

                sectionIds.Add(sid);
            }

            var slots = NormalizeSlots(body.Slots);
            if (slots.Count == 0)
            {
                _errorLog.LogApiError("جدول الحصص: لا حصص", HttpContext);
                return BadRequest(new { message = "أدخل بيانات الحصص." });
            }

            var now = DateTimeOffset.UtcNow;
            var existing = await _db.ClassSchedulePeriods
                .Where(p => p.ClassId == classId && sectionIds.Contains(p.SectionId) && p.DayName == day)
                .ToListAsync(ct);
            _db.ClassSchedulePeriods.RemoveRange(existing);

            foreach (var sectionId in sectionIds)
            {
                foreach (var slot in slots)
                {
                    _db.ClassSchedulePeriods.Add(new ClassSchedulePeriodRecord
                    {
                        Id = Guid.NewGuid(),
                        ClassId = classId,
                        SectionId = sectionId,
                        DayName = day,
                        PeriodNumber = slot.PeriodNumber,
                        SubjectId = slot.SubjectId is null || slot.SubjectId == Guid.Empty ? null : slot.SubjectId,
                        DurationMinutes = slot.DurationMinutes < 1 ? 45 : slot.DurationMinutes,
                        CreatedAt = now,
                        UpdatedAt = now
                    });
                }
            }

            await _db.SaveChangesAsync(ct);
            return Ok(new { message = "تم حفظ جدول الحصص.", sections = sectionIds.Count });
        }
        catch (Exception ex)
        {
            _errorLog.Log(ex, HttpContext);
            return StatusCode(500, new { message = "تعذر حفظ جدول الحصص." });
        }
    }

    [HttpDelete]
    public async Task<ActionResult> DeleteGroup(
        [FromQuery] Guid? classId,
        [FromQuery] Guid? class_id,
        [FromQuery] Guid? sectionId,
        [FromQuery] Guid? section_id,
        [FromQuery] string? dayName,
        [FromQuery] string? day_name,
        CancellationToken ct)
    {
        try
        {
            var cid = classId ?? class_id ?? Guid.Empty;
            var sid = sectionId ?? section_id ?? Guid.Empty;
            var day = (dayName ?? day_name ?? string.Empty).Trim();

            if (cid == Guid.Empty || sid == Guid.Empty || string.IsNullOrWhiteSpace(day))
            {
                return BadRequest(new { message = "معرّف الصف والشعبة واليوم مطلوبة." });
            }

            var rows = await _db.ClassSchedulePeriods
                .Where(p => p.ClassId == cid && p.SectionId == sid && p.DayName == day)
                .ToListAsync(ct);
            if (rows.Count == 0)
            {
                return NotFound();
            }

            _db.ClassSchedulePeriods.RemoveRange(rows);
            await _db.SaveChangesAsync(ct);
            return NoContent();
        }
        catch (Exception ex)
        {
            _errorLog.Log(ex, HttpContext);
            return StatusCode(500, new { message = "تعذر حذف جدول الحصص." });
        }
    }

    private static List<ScheduleSlotRequest> NormalizeSlots(List<ScheduleSlotRequest>? slots)
    {
        if (slots is null || slots.Count == 0)
        {
            return [];
        }

        return slots
            .Select(s => new ScheduleSlotRequest
            {
                PeriodNumber = s.PeriodNumber > 0 ? s.PeriodNumber : s.PeriodNumberAlt,
                SubjectId = s.SubjectId ?? s.SubjectIdAlt,
                DurationMinutes = s.DurationMinutes > 0 ? s.DurationMinutes : s.DurationMinutesAlt
            })
            .Where(s => s.PeriodNumber > 0)
            .OrderBy(s => s.PeriodNumber)
            .ToList();
    }

    public sealed class ScheduleSettingsRequest
    {
        [JsonPropertyName("day_name")]
        public string? DayName { get; set; }

        public string? DayNameAlt { get; set; }

        [JsonPropertyName("periods_count")]
        public int PeriodsCount { get; set; }

        public int PeriodsCountAlt { get; set; }
    }

    public sealed class BulkScheduleRequest
    {
        [JsonPropertyName("class_id")]
        public Guid ClassId { get; set; }

        public Guid ClassIdAlt { get; set; }

        [JsonPropertyName("section_id")]
        public Guid SectionId { get; set; }

        public Guid? SectionIdAlt { get; set; }

        [JsonPropertyName("apply_to_all_sections")]
        public bool ApplyToAllSections { get; set; }

        public bool ApplyToAllSectionsAlt { get; set; }

        [JsonPropertyName("day_name")]
        public string? DayName { get; set; }

        public string? DayNameAlt { get; set; }

        [JsonPropertyName("slots")]
        public List<ScheduleSlotRequest>? Slots { get; set; }
    }

    public sealed class ScheduleSlotRequest
    {
        [JsonPropertyName("period_number")]
        public int PeriodNumber { get; set; }

        public int PeriodNumberAlt { get; set; }

        [JsonPropertyName("subject_id")]
        public Guid? SubjectId { get; set; }

        public Guid? SubjectIdAlt { get; set; }

        [JsonPropertyName("duration_minutes")]
        public int DurationMinutes { get; set; } = 45;

        public int DurationMinutesAlt { get; set; } = 45;
    }
}
