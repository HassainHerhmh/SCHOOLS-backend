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
                dayName = "الأحد";
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
        [FromQuery] string? scheduleDate,
        [FromQuery] string? schedule_date,
        CancellationToken ct)
    {
        try
        {
            var cid = classId ?? class_id;
            var sid = sectionId ?? section_id;
            var day = (dayName ?? day_name ?? string.Empty).Trim();
            var scheduleDateRaw = (scheduleDate ?? schedule_date ?? string.Empty).Trim();
            DateOnly? scheduleDateFilter = null;
            if (!string.IsNullOrEmpty(scheduleDateRaw))
            {
                if (!ScheduleDateHelper.TryParse(scheduleDateRaw, null, out var parsedDate))
                {
                    return BadRequest(new { message = "تاريخ الجدول غير صالح." });
                }

                scheduleDateFilter = parsedDate;
            }

            var q = _db.ClassSchedulePeriods.AsNoTracking();
            if (cid.HasValue)
            {
                q = q.Where(p => p.ClassId == cid.Value);
            }

            if (sid.HasValue)
            {
                q = q.Where(p => p.SectionId == sid.Value);
            }

            if (scheduleDateFilter.HasValue)
            {
                q = q.Where(p => p.ScheduleDate == scheduleDateFilter.Value);
            }
            else if (!string.IsNullOrEmpty(day))
            {
                q = q.Where(p => p.DayName == day);
            }

            var rows = await q
                .OrderBy(p => p.ClassId)
                .ThenBy(p => p.SectionId)
                .ThenByDescending(p => p.ScheduleDate)
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
                schedule_date = ScheduleDateHelper.ToApiString(p.ScheduleDate),
                period_number = p.PeriodNumber,
                subject_id = p.SubjectId,
                subject_name = p.SubjectId.HasValue ? subjectNames.GetValueOrDefault(p.SubjectId.Value) : null,
                duration_minutes = p.DurationMinutes,
                start_hour = p.StartHour,
                start_minute = p.StartMinute,
                end_hour = p.EndHour,
                end_minute = p.EndMinute
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

            if (!ScheduleDateHelper.TryParse(body.ScheduleDate, body.ScheduleDateAlt, out var scheduleDate))
            {
                _errorLog.LogApiError("جدول الحصص: التاريخ فارغ", HttpContext);
                return BadRequest(new { message = "تاريخ الجدول مطلوب." });
            }

            var day = ScheduleDateHelper.ArabicDayName(scheduleDate);

            if (ScheduleDateHelper.TryParse(body.PreviousScheduleDate, body.PreviousScheduleDateAlt, out var previousDate)
                && previousDate != scheduleDate)
            {
                var stalePeriods = await _db.ClassSchedulePeriods
                    .Where(p => p.ClassId == classId && p.ScheduleDate == previousDate)
                    .ToListAsync(ct);
                if (stalePeriods.Count > 0)
                {
                    _db.ClassSchedulePeriods.RemoveRange(stalePeriods);
                }

                var staleCustom = await _db.ClassScheduleCustomItems
                    .Where(p => p.ClassId == classId && p.ScheduleDate == previousDate)
                    .ToListAsync(ct);
                if (staleCustom.Count > 0)
                {
                    _db.ClassScheduleCustomItems.RemoveRange(staleCustom);
                }
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
                var sid = body.SectionId ?? body.SectionIdAlt ?? Guid.Empty;
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
                .Where(p => p.ClassId == classId && sectionIds.Contains(p.SectionId) && p.ScheduleDate == scheduleDate)
                .ToListAsync(ct);
            var existingByKey = existing.ToDictionary(p => (p.SectionId, p.PeriodNumber));
            var touchedIds = new HashSet<Guid>();

            foreach (var sectionId in sectionIds)
            {
                foreach (var slot in slots)
                {
                    var (startH, startM, endH, endM, duration) = ResolveSlotTimes(slot);
                    var subjectId = slot.SubjectId is null || slot.SubjectId == Guid.Empty ? null : slot.SubjectId;
                    var key = (sectionId, slot.PeriodNumber);

                    if (existingByKey.TryGetValue(key, out var row))
                    {
                        row.DayName = day;
                        row.SubjectId = subjectId;
                        row.DurationMinutes = duration;
                        row.StartHour = startH;
                        row.StartMinute = startM;
                        row.EndHour = endH;
                        row.EndMinute = endM;
                        row.UpdatedAt = now;
                        touchedIds.Add(row.Id);
                        continue;
                    }

                    var created = new ClassSchedulePeriodRecord
                    {
                        Id = Guid.NewGuid(),
                        ClassId = classId,
                        SectionId = sectionId,
                        DayName = day,
                        ScheduleDate = scheduleDate,
                        PeriodNumber = slot.PeriodNumber,
                        SubjectId = subjectId,
                        DurationMinutes = duration,
                        StartHour = startH,
                        StartMinute = startM,
                        EndHour = endH,
                        EndMinute = endM,
                        CreatedAt = now,
                        UpdatedAt = now
                    };
                    _db.ClassSchedulePeriods.Add(created);
                    touchedIds.Add(created.Id);
                }
            }

            var orphans = existing.Where(p => !touchedIds.Contains(p.Id)).ToList();
            if (orphans.Count > 0)
            {
                _db.ClassSchedulePeriods.RemoveRange(orphans);
            }

            await TouchScheduleSettingsAsync(now, ct);
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
        [FromQuery] string? scheduleDate,
        [FromQuery] string? schedule_date,
        CancellationToken ct)
    {
        try
        {
            var cid = classId ?? class_id ?? Guid.Empty;
            var sid = sectionId ?? section_id ?? Guid.Empty;
            var scheduleDateRaw = (scheduleDate ?? schedule_date ?? string.Empty).Trim();
            var day = (dayName ?? day_name ?? string.Empty).Trim();

            if (cid == Guid.Empty || sid == Guid.Empty)
            {
                return BadRequest(new { message = "معرّف الصف والشعبة مطلوبة." });
            }

            IQueryable<ClassSchedulePeriodRecord> periodQuery = _db.ClassSchedulePeriods
                .Where(p => p.ClassId == cid && p.SectionId == sid);
            IQueryable<ClassScheduleCustomItemRecord> customQuery = _db.ClassScheduleCustomItems
                .Where(p => p.ClassId == cid && p.SectionId == sid);

            if (ScheduleDateHelper.TryParse(scheduleDateRaw, null, out var parsedDate))
            {
                periodQuery = periodQuery.Where(p => p.ScheduleDate == parsedDate);
                customQuery = customQuery.Where(p => p.ScheduleDate == parsedDate);
            }
            else if (!string.IsNullOrWhiteSpace(day))
            {
                periodQuery = periodQuery.Where(p => p.DayName == day);
                customQuery = customQuery.Where(p => p.DayName == day);
            }
            else
            {
                return BadRequest(new { message = "تاريخ الجدول مطلوب." });
            }

            var rows = await periodQuery.ToListAsync(ct);
            if (rows.Count == 0)
            {
                return NotFound();
            }

            _db.ClassSchedulePeriods.RemoveRange(rows);
            var customRows = await customQuery.ToListAsync(ct);
            if (customRows.Count > 0)
            {
                _db.ClassScheduleCustomItems.RemoveRange(customRows);
            }

            await TouchScheduleSettingsAsync(DateTimeOffset.UtcNow, ct);
            await _db.SaveChangesAsync(ct);
            return NoContent();
        }
        catch (Exception ex)
        {
            _errorLog.Log(ex, HttpContext);
            return StatusCode(500, new { message = "تعذر حذف جدول الحصص." });
        }
    }

    [HttpGet("custom-items")]
    public async Task<ActionResult<IEnumerable<object>>> ListCustomItems(
        [FromQuery] Guid? classId,
        [FromQuery] Guid? class_id,
        [FromQuery] Guid? sectionId,
        [FromQuery] Guid? section_id,
        [FromQuery] string? dayName,
        [FromQuery] string? day_name,
        [FromQuery] string? scheduleDate,
        [FromQuery] string? schedule_date,
        CancellationToken ct)
    {
        try
        {
            var cid = classId ?? class_id;
            var sid = sectionId ?? section_id;
            var day = (dayName ?? day_name ?? string.Empty).Trim();
            var scheduleDateRaw = (scheduleDate ?? schedule_date ?? string.Empty).Trim();
            DateOnly? scheduleDateFilter = null;
            if (!string.IsNullOrEmpty(scheduleDateRaw))
            {
                if (!ScheduleDateHelper.TryParse(scheduleDateRaw, null, out var parsedDate))
                {
                    return BadRequest(new { message = "تاريخ الجدول غير صالح." });
                }

                scheduleDateFilter = parsedDate;
            }

            var q = _db.ClassScheduleCustomItems.AsNoTracking();
            if (cid.HasValue)
            {
                q = q.Where(p => p.ClassId == cid.Value);
            }

            if (sid.HasValue)
            {
                q = q.Where(p => p.SectionId == sid.Value);
            }

            if (scheduleDateFilter.HasValue)
            {
                q = q.Where(p => p.ScheduleDate == scheduleDateFilter.Value);
            }
            else if (!string.IsNullOrEmpty(day))
            {
                q = q.Where(p => p.DayName == day);
            }

            var rows = await q
                .OrderBy(p => p.ClassId)
                .ThenBy(p => p.SectionId)
                .ThenByDescending(p => p.ScheduleDate)
                .ThenBy(p => p.PositionNumber)
                .ToListAsync(ct);

            if (rows.Count == 0)
            {
                return Ok(Array.Empty<object>());
            }

            var sectionIds = rows.Select(r => r.SectionId).Distinct().ToList();
            var sectionNames = await _db.SchoolSections.AsNoTracking()
                .Where(s => sectionIds.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id, s => s.Name, ct);

            return Ok(rows.Select(p => new
            {
                id = p.Id,
                class_id = p.ClassId,
                section_id = p.SectionId,
                section_name = sectionNames.GetValueOrDefault(p.SectionId),
                day_name = p.DayName,
                schedule_date = ScheduleDateHelper.ToApiString(p.ScheduleDate),
                item_name = p.ItemName,
                position_number = p.PositionNumber,
                start_hour = p.StartHour,
                start_minute = p.StartMinute,
                end_hour = p.EndHour,
                end_minute = p.EndMinute
            }));
        }
        catch (Exception ex)
        {
            _errorLog.Log(ex, HttpContext);
            return StatusCode(500, new { message = "تعذر تحميل بنود الجدول." });
        }
    }

    [HttpPost("custom-items")]
    public async Task<ActionResult<object>> CreateCustomItem([FromBody] CustomItemRequest? body, CancellationToken ct)
    {
        try
        {
            if (body is null)
            {
                return BadRequest(new { message = "بيانات البند مطلوبة." });
            }

            var validation = ValidateCustomItemBody(body, isUpdate: false);
            if (validation.Error is not null)
            {
                return validation.Error;
            }

            var targets = await ResolveCustomItemTargetsAsync(body, ct);
            if (targets.Count == 0)
            {
                return BadRequest(new { message = "لا توجد شعب لتطبيق البند عليها." });
            }

            var now = DateTimeOffset.UtcNow;
            var created = new List<ClassScheduleCustomItemRecord>();
            foreach (var (targetClassId, targetSectionId) in targets)
            {
                created.Add(new ClassScheduleCustomItemRecord
                {
                    Id = Guid.NewGuid(),
                    ClassId = targetClassId,
                    SectionId = targetSectionId,
                    DayName = validation.Day!,
                    ScheduleDate = validation.ScheduleDate,
                    ItemName = validation.ItemName!,
                    PositionNumber = validation.Position,
                    StartHour = validation.StartH,
                    StartMinute = validation.StartM,
                    EndHour = validation.EndH,
                    EndMinute = validation.EndM,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }

            _db.ClassScheduleCustomItems.AddRange(created);
            await TouchScheduleSettingsAsync(now, ct);
            await _db.SaveChangesAsync(ct);

            var first = created[0];
            return Ok(new
            {
                id = first.Id,
                class_id = first.ClassId,
                section_id = first.SectionId,
                day_name = first.DayName,
                schedule_date = ScheduleDateHelper.ToApiString(first.ScheduleDate),
                item_name = first.ItemName,
                position_number = first.PositionNumber,
                start_hour = first.StartHour,
                start_minute = first.StartMinute,
                end_hour = first.EndHour,
                end_minute = first.EndMinute,
                created_count = created.Count
            });
        }
        catch (Exception ex)
        {
            _errorLog.Log(ex, HttpContext);
            return StatusCode(500, new { message = "تعذر حفظ البند." });
        }
    }

    [HttpPut("custom-items/{id:guid}")]
    public async Task<ActionResult<object>> UpdateCustomItem(Guid id, [FromBody] CustomItemRequest? body, CancellationToken ct)
    {
        try
        {
            if (body is null)
            {
                return BadRequest(new { message = "بيانات البند مطلوبة." });
            }

            var row = await _db.ClassScheduleCustomItems.FirstOrDefaultAsync(p => p.Id == id, ct);
            if (row is null)
            {
                return NotFound();
            }

            var validation = ValidateCustomItemBody(body, isUpdate: true);
            if (validation.Error is not null)
            {
                return validation.Error;
            }

            row.ItemName = validation.ItemName!;
            row.DayName = validation.Day!;
            row.ScheduleDate = validation.ScheduleDate;
            row.PositionNumber = validation.Position;
            row.StartHour = validation.StartH;
            row.StartMinute = validation.StartM;
            row.EndHour = validation.EndH;
            row.EndMinute = validation.EndM;
            row.UpdatedAt = DateTimeOffset.UtcNow;

            var sectionId = body.SectionId != Guid.Empty ? body.SectionId : body.SectionIdAlt;
            if (sectionId != Guid.Empty)
            {
                row.SectionId = sectionId;
            }

            await TouchScheduleSettingsAsync(row.UpdatedAt, ct);
            await _db.SaveChangesAsync(ct);

            return Ok(new
            {
                id = row.Id,
                class_id = row.ClassId,
                section_id = row.SectionId,
                day_name = row.DayName,
                schedule_date = ScheduleDateHelper.ToApiString(row.ScheduleDate),
                item_name = row.ItemName,
                position_number = row.PositionNumber,
                start_hour = row.StartHour,
                start_minute = row.StartMinute,
                end_hour = row.EndHour,
                end_minute = row.EndMinute
            });
        }
        catch (Exception ex)
        {
            _errorLog.Log(ex, HttpContext);
            return StatusCode(500, new { message = "تعذر تحديث البند." });
        }
    }

    [HttpDelete("custom-items/{id:guid}")]
    public async Task<ActionResult> DeleteCustomItem(Guid id, CancellationToken ct)
    {
        try
        {
            var row = await _db.ClassScheduleCustomItems.FirstOrDefaultAsync(p => p.Id == id, ct);
            if (row is null)
            {
                return NotFound();
            }

            _db.ClassScheduleCustomItems.Remove(row);
            await TouchScheduleSettingsAsync(DateTimeOffset.UtcNow, ct);
            await _db.SaveChangesAsync(ct);
            return NoContent();
        }
        catch (Exception ex)
        {
            _errorLog.Log(ex, HttpContext);
            return StatusCode(500, new { message = "تعذر حذف البند." });
        }
    }

    private async Task TouchScheduleSettingsAsync(DateTimeOffset updatedAt, CancellationToken ct)
    {
        var row = await _db.ClassScheduleSettings.FirstOrDefaultAsync(s => s.Id == 1, ct);
        if (row is null)
        {
            _db.ClassScheduleSettings.Add(new ClassScheduleSettingsRecord
            {
                Id = 1,
                DayName = "الأحد",
                PeriodsCount = 6,
                UpdatedAt = updatedAt
            });
            return;
        }

        row.UpdatedAt = updatedAt;
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
                DurationMinutes = s.DurationMinutes > 0 ? s.DurationMinutes : s.DurationMinutesAlt,
                StartHour = s.StartHour ?? s.StartHourAlt,
                StartMinute = s.StartMinute ?? s.StartMinuteAlt,
                EndHour = s.EndHour ?? s.EndHourAlt,
                EndMinute = s.EndMinute ?? s.EndMinuteAlt
            })
            .Where(s => s.PeriodNumber > 0)
            .OrderBy(s => s.PeriodNumber)
            .ToList();
    }

    private static (int startH, int startM, int endH, int endM, int duration) ResolveSlotTimes(ScheduleSlotRequest slot)
    {
        if (TryParseTime(slot.StartHour, slot.StartMinute, null, null, out var startH, out var startM)
            && TryParseTime(slot.EndHour, slot.EndMinute, null, null, out var endH, out var endM)
            && startH * 60 + startM < endH * 60 + endM)
        {
            var duration = endH * 60 + endM - (startH * 60 + startM);
            return (startH, startM, endH, endM, duration < 1 ? 45 : duration);
        }

        var fallback = slot.DurationMinutes < 1 ? 45 : slot.DurationMinutes;
        var baseStart = 7 * 60 + (slot.PeriodNumber - 1) * fallback;
        var sh = baseStart / 60;
        var sm = baseStart % 60;
        var endTotal = baseStart + fallback;
        return (sh, sm, endTotal / 60, endTotal % 60, fallback);
    }

    private async Task<List<(Guid ClassId, Guid SectionId)>> ResolveCustomItemTargetsAsync(
        CustomItemRequest body,
        CancellationToken ct)
    {
        var classId = body.ClassId != Guid.Empty ? body.ClassId : body.ClassIdAlt;
        var sectionId = body.SectionId != Guid.Empty ? body.SectionId : body.SectionIdAlt;
        var applyAllClasses = body.ApplyToAllClasses || body.ApplyToAllClassesAlt;
        var applyAllSections = body.ApplyToAllSections || body.ApplyToAllSectionsAlt;

        if (applyAllClasses)
        {
            return await _db.SchoolSections.AsNoTracking()
                .Select(s => new ValueTuple<Guid, Guid>(s.ClassId, s.Id))
                .ToListAsync(ct);
        }

        if (applyAllSections)
        {
            if (classId == Guid.Empty)
            {
                return [];
            }

            return await _db.SchoolSections.AsNoTracking()
                .Where(s => s.ClassId == classId)
                .Select(s => new ValueTuple<Guid, Guid>(s.ClassId, s.Id))
                .ToListAsync(ct);
        }

        if (classId == Guid.Empty || sectionId == Guid.Empty)
        {
            return [];
        }

        return [(classId, sectionId)];
    }

    private static (ActionResult? Error, DateOnly ScheduleDate, string? Day, string? ItemName, int Position, int StartH, int StartM, int EndH, int EndM)
        ValidateCustomItemBody(CustomItemRequest body, bool isUpdate)
    {
        var classId = body.ClassId != Guid.Empty ? body.ClassId : body.ClassIdAlt;
        var sectionId = body.SectionId != Guid.Empty ? body.SectionId : body.SectionIdAlt;
        var applyAllClasses = body.ApplyToAllClasses || body.ApplyToAllClassesAlt;
        var applyAllSections = body.ApplyToAllSections || body.ApplyToAllSectionsAlt;
        var itemName = (body.ItemName ?? body.ItemNameAlt ?? string.Empty).Trim();
        var position = body.PositionNumber > 0 ? body.PositionNumber : body.PositionNumberAlt;
        var emptyDate = default(DateOnly);

        if (!isUpdate && !applyAllClasses && !applyAllSections && (classId == Guid.Empty || sectionId == Guid.Empty))
        {
            return (new BadRequestObjectResult(new { message = "الصف والشعبة مطلوبة." }), emptyDate, null, null, 0, 0, 0, 0, 0);
        }

        if (!isUpdate && !applyAllClasses && applyAllSections && classId == Guid.Empty)
        {
            return (new BadRequestObjectResult(new { message = "الصف مطلوب." }), emptyDate, null, null, 0, 0, 0, 0, 0);
        }

        if (!ScheduleDateHelper.TryParse(body.ScheduleDate, body.ScheduleDateAlt, out var scheduleDate))
        {
            return (new BadRequestObjectResult(new { message = "تاريخ الجدول مطلوب." }), emptyDate, null, null, 0, 0, 0, 0, 0);
        }

        var day = ScheduleDateHelper.ArabicDayName(scheduleDate);

        if (string.IsNullOrWhiteSpace(itemName))
        {
            return (new BadRequestObjectResult(new { message = "اسم البند مطلوب." }), emptyDate, null, null, 0, 0, 0, 0, 0);
        }

        if (position < 1 || position > 20)
        {
            return (new BadRequestObjectResult(new { message = "ترتيب البند يجب أن يكون بين 1 و 20." }), emptyDate, null, null, 0, 0, 0, 0, 0);
        }

        if (!TryParseTime(body.StartHour, body.StartMinute, body.StartHourAlt, body.StartMinuteAlt, out var startH, out var startM)
            || !TryParseTime(body.EndHour, body.EndMinute, body.EndHourAlt, body.EndMinuteAlt, out var endH, out var endM))
        {
            return (new BadRequestObjectResult(new { message = "وقت البداية والنهاية مطلوبان." }), emptyDate, null, null, 0, 0, 0, 0, 0);
        }

        if (startH * 60 + startM >= endH * 60 + endM)
        {
            return (new BadRequestObjectResult(new { message = "وقت النهاية يجب أن يكون بعد وقت البداية." }), emptyDate, null, null, 0, 0, 0, 0, 0);
        }

        return (null, scheduleDate, day, itemName, position, startH, startM, endH, endM);
    }

    private static bool TryParseTime(int? hour, int? minute, int? hourAlt, int? minuteAlt, out int h, out int m)
    {
        h = hour ?? hourAlt ?? -1;
        m = minute ?? minuteAlt ?? -1;
        if (h < 0 || h > 23 || m < 0 || m > 59)
        {
            return false;
        }

        return true;
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
        public Guid? SectionId { get; set; }

        public Guid? SectionIdAlt { get; set; }

        [JsonPropertyName("apply_to_all_sections")]
        public bool ApplyToAllSections { get; set; }

        public bool ApplyToAllSectionsAlt { get; set; }

        [JsonPropertyName("day_name")]
        public string? DayName { get; set; }

        public string? DayNameAlt { get; set; }

        [JsonPropertyName("schedule_date")]
        public string? ScheduleDate { get; set; }

        public string? ScheduleDateAlt { get; set; }

        [JsonPropertyName("previous_schedule_date")]
        public string? PreviousScheduleDate { get; set; }

        public string? PreviousScheduleDateAlt { get; set; }

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

        [JsonPropertyName("start_hour")]
        public int? StartHour { get; set; }

        public int? StartHourAlt { get; set; }

        [JsonPropertyName("start_minute")]
        public int? StartMinute { get; set; }

        public int? StartMinuteAlt { get; set; }

        [JsonPropertyName("end_hour")]
        public int? EndHour { get; set; }

        public int? EndHourAlt { get; set; }

        [JsonPropertyName("end_minute")]
        public int? EndMinute { get; set; }

        public int? EndMinuteAlt { get; set; }
    }

    public sealed class CustomItemRequest
    {
        [JsonPropertyName("class_id")]
        public Guid ClassId { get; set; }

        public Guid ClassIdAlt { get; set; }

        [JsonPropertyName("section_id")]
        public Guid SectionId { get; set; }

        public Guid SectionIdAlt { get; set; }

        [JsonPropertyName("day_name")]
        public string? DayName { get; set; }

        public string? DayNameAlt { get; set; }

        [JsonPropertyName("schedule_date")]
        public string? ScheduleDate { get; set; }

        public string? ScheduleDateAlt { get; set; }

        [JsonPropertyName("item_name")]
        public string? ItemName { get; set; }

        public string? ItemNameAlt { get; set; }

        [JsonPropertyName("position_number")]
        public int PositionNumber { get; set; }

        public int PositionNumberAlt { get; set; }

        [JsonPropertyName("start_hour")]
        public int? StartHour { get; set; }

        public int? StartHourAlt { get; set; }

        [JsonPropertyName("start_minute")]
        public int? StartMinute { get; set; }

        public int? StartMinuteAlt { get; set; }

        [JsonPropertyName("end_hour")]
        public int? EndHour { get; set; }

        public int? EndHourAlt { get; set; }

        [JsonPropertyName("end_minute")]
        public int? EndMinute { get; set; }

        public int? EndMinuteAlt { get; set; }

        [JsonPropertyName("apply_to_all_sections")]
        public bool ApplyToAllSections { get; set; }

        public bool ApplyToAllSectionsAlt { get; set; }

        [JsonPropertyName("apply_to_all_classes")]
        public bool ApplyToAllClasses { get; set; }

        public bool ApplyToAllClassesAlt { get; set; }
    }
}
