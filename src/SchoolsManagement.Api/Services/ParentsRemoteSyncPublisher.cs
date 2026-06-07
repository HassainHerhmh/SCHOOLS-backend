using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SchoolsManagement.Api.Data;
using SchoolsManagement.Api.Models.School;

namespace SchoolsManagement.Api.Services;

/// <summary>رفع بيانات المدرسة المحلية إلى سيرفر رويال الخارجي (تطبيق الآباء).</summary>
public class ParentsRemoteSyncPublisher
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    public ParentsRemoteSyncPublisher(
        ApplicationDbContext db,
        IConfiguration config,
        IHttpClientFactory httpClientFactory)
    {
        _db = db;
        _config = config;
        _httpClientFactory = httpClientFactory;
    }

    public (string RemoteUrl, string SyncKey, string? SchoolId) GetRemoteSettings()
    {
        var url = _config["ParentsRoyal:RemoteApiUrl"]?.Trim().TrimEnd('/');
        var key = _config["ParentsRoyal:SyncApiKey"]?.Trim();
        var schoolId = _config["ParentsRoyal:SchoolId"]?.Trim();
        return (url ?? string.Empty, key ?? string.Empty, string.IsNullOrWhiteSpace(schoolId) ? null : schoolId);
    }

    public bool IsConfigured()
    {
        var (url, key, _) = GetRemoteSettings();
        return !string.IsNullOrWhiteSpace(url)
               && !string.IsNullOrWhiteSpace(key)
               && !url.Contains("YOUR_ROYAL", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<ParentsIngestResult> PublishAsync(
        ParentsSyncPlan plan,
        Action<int, int, string>? onProgress,
        CancellationToken cancellationToken = default)
    {
        var (remoteUrl, syncKey, schoolId) = GetRemoteSettings();
        if (string.IsNullOrWhiteSpace(remoteUrl) || string.IsNullOrWhiteSpace(syncKey))
        {
            throw new InvalidOperationException(
                "لم يُضبط السيرفر الخارجي. أضف ParentsRoyal:RemoteApiUrl و ParentsRoyal:SyncApiKey في appsettings.Secrets.json");
        }

        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromMinutes(10);
        var ingestUrl = $"{remoteUrl}/api/sync/ingest-parents";
        var totalItems = Math.Max(1, plan.TotalItems);
        var uploadedItems = 0;
        var aggregate = new ParentsIngestResult();

        if (plan.SyncStudents)
        {
            onProgress?.Invoke(uploadedItems, totalItems, "جاري رفع بيانات الطلاب إلى السيرفر الخارجي");
            var students = await LoadStudentsAsync(plan, cancellationToken);
            PaymentInstallmentSettingsRecord? installmentSettings = null;
            if (plan.SyncInstallments)
            {
                installmentSettings = await _db.PaymentInstallmentSettings.AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == 1, cancellationToken)
                    ?? new PaymentInstallmentSettingsRecord
                    {
                        Id = 1,
                        TuitionInstallmentsCount = 6,
                        BusInstallmentsCount = 2
                    };
            }
            var chunkSize = 40;
            for (var i = 0; i < students.Count; i += chunkSize)
            {
                var chunk = students.Skip(i).Take(chunkSize).ToList();
                var studentIds = chunk.Select(s => s.Id).ToList();
                var reportChunk = await LoadStudentReportsAsync(plan, studentIds, cancellationToken);
                List<ParentsInstallmentIngestDto>? installmentChunk = null;
                if (plan.SyncInstallments && installmentSettings is not null)
                {
                    installmentChunk = await LoadInstallmentsAsync(plan, studentIds, installmentSettings, cancellationToken);
                }

                var result = await PostIngestAsync(client, ingestUrl, syncKey, schoolId, new ParentsSyncIngestPayload
                {
                    SchoolId = schoolId,
                    Students = chunk,
                    StudentReports = reportChunk,
                    Installments = installmentChunk
                }, cancellationToken);
                aggregate.Students += result.Students;
                aggregate.StudentReports += result.StudentReports;
                aggregate.Installments += result.Installments;
                if (chunk.Count > 0 && result.Students <= 0)
                {
                    throw new InvalidOperationException(
                        $"السيرفر الخارجي لم يحفظ دفعة الطلاب ({chunk.Count} سجل) — تحقق من جدول parents_students_summary على MySQL.");
                }

                if (reportChunk.Count > 0 && result.StudentReports <= 0)
                {
                    throw new InvalidOperationException(
                        $"السيرفر الخارجي لم يحفظ تقارير الطلاب ({reportChunk.Count} سجل) — تحقق من جدول parents_student_reports على MySQL.");
                }

                if (installmentChunk is { Count: > 0 } && result.Installments <= 0)
                {
                    throw new InvalidOperationException(
                        $"السيرفر الخارجي لم يحفظ أقساط الطلاب ({installmentChunk.Count} سجل) — تحقق من جدول parents_student_installments على MySQL.");
                }

                uploadedItems += chunk.Count;
                onProgress?.Invoke(uploadedItems, totalItems, $"تم رفع {uploadedItems} من {totalItems} {plan.ItemLabel}");
            }
        }

        if (plan.SyncClasses)
        {
            onProgress?.Invoke(uploadedItems, totalItems, "جاري رفع بيانات الصفوف");
            var classes = await LoadClassesAsync(plan, cancellationToken);
            if (classes.Count > 0)
            {
                var result = await PostIngestAsync(client, ingestUrl, syncKey, schoolId, new ParentsSyncIngestPayload
                {
                    SchoolId = schoolId,
                    Classes = classes
                }, cancellationToken);
                aggregate.Classes += result.Classes;
                if (classes.Count > 0 && result.Classes <= 0)
                {
                    throw new InvalidOperationException("السيرفر الخارجي لم يحفظ بيانات الصفوف.");
                }

                uploadedItems += plan.ChangedClasses;
                onProgress?.Invoke(uploadedItems, totalItems, "تم رفع الصفوف");
            }
        }

        if (plan.SyncSections)
        {
            onProgress?.Invoke(uploadedItems, totalItems, "جاري رفع بيانات الشعب");
            var sections = await LoadSectionsAsync(plan, cancellationToken);
            if (sections.Count > 0)
            {
                var result = await PostIngestAsync(client, ingestUrl, syncKey, schoolId, new ParentsSyncIngestPayload
                {
                    SchoolId = schoolId,
                    Sections = sections
                }, cancellationToken);
                aggregate.Sections += result.Sections;
                if (sections.Count > 0 && result.Sections <= 0)
                {
                    throw new InvalidOperationException("السيرفر الخارجي لم يحفظ بيانات الشعب.");
                }

                uploadedItems += plan.ChangedSections;
                onProgress?.Invoke(uploadedItems, totalItems, "تم رفع الشعب");
            }
        }

        if (plan.SyncAttendance)
        {
            onProgress?.Invoke(uploadedItems, totalItems, "جاري رفع بيانات الحضور");
            var attendance = await LoadAttendanceAsync(plan, cancellationToken);
            var chunkSize = 80;
            for (var i = 0; i < attendance.Count; i += chunkSize)
            {
                var chunk = attendance.Skip(i).Take(chunkSize).ToList();
                var result = await PostIngestAsync(client, ingestUrl, syncKey, schoolId, new ParentsSyncIngestPayload
                {
                    SchoolId = schoolId,
                    Attendance = chunk
                }, cancellationToken);
                aggregate.Attendance += result.Attendance;
                if (chunk.Count > 0 && result.Attendance <= 0)
                {
                    throw new InvalidOperationException("السيرفر الخارجي لم يحفظ دفعة الحضور.");
                }

                uploadedItems += chunk.Count;
                onProgress?.Invoke(uploadedItems, totalItems, $"تم رفع {uploadedItems} من {totalItems} {plan.ItemLabel}");
            }
        }

        if (plan.SyncSchedule)
        {
            onProgress?.Invoke(uploadedItems, totalItems, "جاري رفع جدول الحصص");
            var scheduleEntries = await LoadSchedulePeriodsAsync(cancellationToken);
            var scheduleSettings = await LoadScheduleSettingsAsync(cancellationToken);
            var result = await PostIngestAsync(client, ingestUrl, syncKey, schoolId, new ParentsSyncIngestPayload
            {
                SchoolId = schoolId,
                SchedulePeriods = scheduleEntries,
                ScheduleFullReplace = true,
                ScheduleSettings = scheduleSettings
            }, cancellationToken);
            aggregate.SchedulePeriods += result.SchedulePeriods;
            aggregate.ScheduleSettings += result.ScheduleSettings;
            if (scheduleEntries.Count > 0 && result.SchedulePeriods <= 0)
            {
                throw new InvalidOperationException("السيرفر الخارجي لم يحفظ جدول الحصص.");
            }

            uploadedItems += plan.ChangedSchedule;
            onProgress?.Invoke(uploadedItems, totalItems, "تم رفع جدول الحصص");
        }

        return aggregate;
    }

    public async Task<ParentsRemoteDataCounts> FetchRemoteCountsAsync(CancellationToken cancellationToken = default)
    {
        var (remoteUrl, syncKey, _) = GetRemoteSettings();
        if (string.IsNullOrWhiteSpace(remoteUrl) || string.IsNullOrWhiteSpace(syncKey))
        {
            throw new InvalidOperationException("لم يُضبط السيرفر الخارجي.");
        }

        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(60);
        var statusUrl = $"{remoteUrl}/api/sync/parents-data-status";
        using var request = new HttpRequestMessage(HttpMethod.Get, statusUrl);
        request.Headers.Add("X-Parents-Sync-Key", syncKey);

        using var response = await client.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(FormatRemoteHttpFailure(response.StatusCode, body, statusUrl));
        }

        var counts = JsonSerializer.Deserialize<ParentsRemoteDataCounts>(body, JsonOptions);
        return counts ?? new ParentsRemoteDataCounts();
    }

    private async Task<ParentsIngestResult> PostIngestAsync(
        HttpClient client,
        string ingestUrl,
        string syncKey,
        string? schoolId,
        ParentsSyncIngestPayload payload,
        CancellationToken cancellationToken)
    {
        payload.SchoolId ??= schoolId;
        using var request = new HttpRequestMessage(HttpMethod.Post, ingestUrl);
        request.Headers.Add("X-Parents-Sync-Key", syncKey);
        request.Content = new StringContent(
            JsonSerializer.Serialize(payload, JsonOptions),
            Encoding.UTF8,
            "application/json");

        using var response = await client.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(FormatRemoteHttpFailure(response.StatusCode, body, ingestUrl));
        }

        var result = JsonSerializer.Deserialize<ParentsIngestResult>(body, JsonOptions);
        return result ?? new ParentsIngestResult();
    }

    private async Task<List<ParentsStudentReportIngestDto>> LoadStudentReportsAsync(
        ParentsSyncPlan plan,
        IReadOnlyList<Guid> studentIds,
        CancellationToken ct)
    {
        if (studentIds.Count == 0)
        {
            return [];
        }

        var students = await _db.StudentRecords
            .Where(s => s.Status == "active" && studentIds.Contains(s.Id))
            .ToListAsync(ct);

        var discounts = await _db.StudentDiscountApplications
            .Where(a => studentIds.Contains(a.StudentId))
            .GroupBy(a => a.StudentId)
            .Select(g => new { StudentId = g.Key, Total = g.Sum(a => a.Amount) })
            .ToDictionaryAsync(x => x.StudentId, x => x.Total, ct);

        return students
            .Select(s => ParentsStudentReportCalculator.FromStudent(s, discounts.GetValueOrDefault(s.Id)))
            .ToList();
    }

    private async Task<List<ParentsStudentIngestDto>> LoadStudentsAsync(ParentsSyncPlan plan, CancellationToken ct)
    {
        var query = _db.StudentRecords.Where(s => s.Status == "active");
        if (plan.StudentsSince is not null)
        {
            query = query.Where(s =>
                (s.UpdatedAt ?? s.CreatedAt ?? DateTimeOffset.MinValue) > plan.StudentsSince.Value);
        }

        return await query
            .Select(s => new ParentsStudentIngestDto
            {
                Id = s.Id,
                ParentPhone = s.ParentPhone ?? s.Phone,
                Email = s.Email,
                Name = s.Name,
                Level = s.Level,
                Section = s.Section,
                PaidAmount = s.PaidAmount,
                SchoolFees = s.SchoolFees,
                UniformFees = s.UniformFees,
                BusFees = s.BusFees,
                BooksFees = s.BooksFees
            })
            .ToListAsync(ct);
    }

    private async Task<List<ParentsClassIngestDto>> LoadClassesAsync(ParentsSyncPlan plan, CancellationToken ct)
    {
        var query = _db.GradeClasses.AsQueryable();
        if (plan.ClassesSince is not null)
        {
            query = query.Where(c =>
                (c.UpdatedAt ?? c.CreatedAt ?? DateTimeOffset.MinValue) > plan.ClassesSince.Value);
        }

        return await query
            .Select(c => new ParentsClassIngestDto
            {
                Id = c.Id,
                Name = c.Name,
                Level = c.Level.ToString(),
                DisplayOrder = c.DisplayOrder,
                TuitionFees = c.TuitionFees,
                UniformFees = c.UniformFees,
                BusFees = c.BusFees,
                BooksFees = c.BooksFees
            })
            .ToListAsync(ct);
    }

    private async Task<List<ParentsSectionIngestDto>> LoadSectionsAsync(ParentsSyncPlan plan, CancellationToken ct)
    {
        var query = _db.SchoolSections.AsQueryable();
        if (plan.SectionsSince is not null)
        {
            query = query.Where(s =>
                (s.UpdatedAt ?? s.CreatedAt ?? DateTimeOffset.MinValue) > plan.SectionsSince.Value);
        }

        return await query
            .Select(s => new ParentsSectionIngestDto
            {
                Id = s.Id,
                Name = s.Name,
                ClassId = s.ClassId,
                TeacherId = s.TeacherId,
                TeacherName = s.TeacherName
            })
            .ToListAsync(ct);
    }

    private async Task<List<ParentsInstallmentIngestDto>> LoadInstallmentsAsync(
        ParentsSyncPlan plan,
        IReadOnlyList<Guid> studentIds,
        PaymentInstallmentSettingsRecord settings,
        CancellationToken ct)
    {
        if (studentIds.Count == 0)
        {
            return [];
        }

        var students = await _db.StudentRecords
            .AsNoTracking()
            .Where(s => s.Status == "active" && studentIds.Contains(s.Id))
            .ToListAsync(ct);

        return students
            .SelectMany(s => ParentsInstallmentSyncHelper.BuildForStudent(s, settings))
            .ToList();
    }

    private async Task<List<ParentsSchedulePeriodIngestDto>> LoadSchedulePeriodsAsync(CancellationToken ct)
    {
        var periodRows = await _db.ClassSchedulePeriods.AsNoTracking()
            .OrderBy(p => p.ClassId)
            .ThenBy(p => p.SectionId)
            .ThenBy(p => p.ScheduleDate)
            .ThenBy(p => p.PeriodNumber)
            .ToListAsync(ct);

        var customRows = await _db.ClassScheduleCustomItems.AsNoTracking()
            .OrderBy(p => p.ClassId)
            .ThenBy(p => p.SectionId)
            .ThenBy(p => p.ScheduleDate)
            .ThenBy(p => p.PositionNumber)
            .ToListAsync(ct);

        if (periodRows.Count == 0 && customRows.Count == 0)
        {
            return [];
        }

        var sectionIds = periodRows.Select(r => r.SectionId)
            .Concat(customRows.Select(r => r.SectionId))
            .Distinct()
            .ToList();
        var subjectIds = periodRows.Where(r => r.SubjectId.HasValue).Select(r => r.SubjectId!.Value).Distinct().ToList();

        var sectionNames = sectionIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _db.SchoolSections.AsNoTracking()
                .Where(s => sectionIds.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id, s => s.Name, ct);

        var subjectNames = subjectIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _db.Subjects.AsNoTracking()
                .Where(s => subjectIds.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id, s => s.Name, ct);

        var periods = periodRows.Select(p => new ParentsSchedulePeriodIngestDto
        {
            Id = p.Id,
            ClassId = p.ClassId,
            SectionId = p.SectionId,
            SectionName = sectionNames.GetValueOrDefault(p.SectionId),
            DayName = p.DayName,
            ScheduleDate = ScheduleDateHelper.ToApiString(p.ScheduleDate),
            PeriodNumber = p.PeriodNumber,
            EntryKind = "period",
            SubjectId = p.SubjectId,
            SubjectName = p.SubjectId.HasValue ? subjectNames.GetValueOrDefault(p.SubjectId.Value) : null,
            DurationMinutes = p.DurationMinutes,
            StartHour = p.StartHour,
            StartMinute = p.StartMinute,
            EndHour = p.EndHour,
            EndMinute = p.EndMinute
        });

        var customs = customRows.Select(p => new ParentsSchedulePeriodIngestDto
        {
            Id = p.Id,
            ClassId = p.ClassId,
            SectionId = p.SectionId,
            SectionName = sectionNames.GetValueOrDefault(p.SectionId),
            DayName = p.DayName,
            ScheduleDate = ScheduleDateHelper.ToApiString(p.ScheduleDate),
            PeriodNumber = p.PositionNumber,
            EntryKind = "custom",
            ItemName = p.ItemName,
            DurationMinutes = 0,
            StartHour = p.StartHour,
            StartMinute = p.StartMinute,
            EndHour = p.EndHour,
            EndMinute = p.EndMinute
        });

        return periods.Concat(customs).ToList();
    }

    private async Task<ParentsScheduleSettingsIngestDto?> LoadScheduleSettingsAsync(CancellationToken ct)
    {
        var row = await _db.ClassScheduleSettings.AsNoTracking().FirstOrDefaultAsync(s => s.Id == 1, ct);
        if (row is null)
        {
            return new ParentsScheduleSettingsIngestDto();
        }

        return new ParentsScheduleSettingsIngestDto
        {
            DayName = row.DayName,
            PeriodsCount = row.PeriodsCount
        };
    }

    private async Task<List<ParentsAttendanceIngestDto>> LoadAttendanceAsync(ParentsSyncPlan plan, CancellationToken ct)
    {
        var activeStudentIds = await _db.StudentRecords
            .Where(s => s.Status == "active")
            .Select(s => s.Id)
            .ToListAsync(ct);

        var query = _db.AttendanceRecords.Where(a => activeStudentIds.Contains(a.StudentId));
        if (plan.AttendanceSince is not null)
        {
            query = query.Where(a => a.CreatedAt > plan.AttendanceSince.Value);
        }

        return await query
            .Select(a => new ParentsAttendanceIngestDto
            {
                StudentId = a.StudentId,
                Date = a.Date.ToString("yyyy-MM-dd"),
                Status = a.Status
            })
            .ToListAsync(ct);
    }

    private static string FormatRemoteHttpFailure(System.Net.HttpStatusCode status, string body, string ingestUrl)
    {
        var code = (int)status;
        var detail = ExtractJsonErrorMessage(body) ?? body.Trim();
        if (string.IsNullOrWhiteSpace(detail))
        {
            detail = "(لا يوجد نص تفصيلي من السيرفر)";
        }

        if (detail.Length > 1500)
        {
            detail = detail[..1500] + "…";
        }

        return $"السيرفر الخارجي ({code}) — {detail} — {ingestUrl}";
    }

    private static string? ExtractJsonErrorMessage(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            foreach (var key in new[] { "message", "error", "detail", "title" })
            {
                if (root.TryGetProperty(key, out var prop) && prop.ValueKind == JsonValueKind.String)
                {
                    var text = prop.GetString();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        return text;
                    }
                }
            }
        }
        catch
        {
            /* ليس JSON */
        }

        return null;
    }

    public sealed class ParentsSyncPlan
    {
        public int ChangedStudents { get; set; }
        public int ChangedClasses { get; set; }
        public int ChangedSections { get; set; }
        public int ChangedAttendance { get; set; }
        public int ChangedInstallments { get; set; }
        public int ChangedSchedule { get; set; }
        public bool SyncStudents { get; set; }
        public bool SyncClasses { get; set; }
        public bool SyncSections { get; set; }
        public bool SyncAttendance { get; set; }
        public bool SyncStudentReports { get; set; }
        public bool SyncInstallments { get; set; }
        public bool SyncSchedule { get; set; }
        public int ChangedStudentReports { get; set; }
        public int TotalItems { get; set; }
        public string ItemLabel { get; set; } = "عنصر";
        public DateTimeOffset? AttendanceSince { get; set; }
        public DateTimeOffset? StudentsSince { get; set; }
        public DateTimeOffset? ClassesSince { get; set; }
        public DateTimeOffset? SectionsSince { get; set; }
        public DateTimeOffset? ScheduleSince { get; set; }
        public DateTimeOffset CheckpointAt { get; set; }
        public bool HasChanges => TotalItems > 0;
    }
}
