using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolsManagement.Api.Data;

namespace SchoolsManagement.Api.Controllers;

[ApiController]
[Route("api/sync")]
[AllowAnonymous] // في الإنتاج يجب أن يكون متاحاً فقط للمسؤولين
public class SyncController : ControllerBase
{
    private const string StudentsCheckpointKey = "parents.students";
    private const string ClassesCheckpointKey = "parents.classes";
    private const string SectionsCheckpointKey = "parents.sections";
    private const string AttendanceCheckpointKey = "parents.attendance";

    private static readonly ConcurrentDictionary<string, ParentsSyncProgressState> ParentsSyncProgressBySession = new();

    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _config;
    private readonly HttpClient _httpClient;

    public SyncController(ApplicationDbContext db, IConfiguration config, IHttpClientFactory httpClientFactory)
    {
        _db = db;
        _config = config;
        _httpClient = httpClientFactory.CreateClient();
    }

    /// <summary>عدد الطلاب النشطين المعروض في واجهة المزامنة (شريط التقدّم).</summary>
    [HttpGet("parents-sync-preview")]
    public async Task<IActionResult> ParentsSyncPreview(CancellationToken cancellationToken)
    {
        var plan = await BuildParentsSyncPlan(cancellationToken);
        return Ok(new
        {
            student_count = plan.TotalItems,
            total_items = plan.TotalItems,
            item_label = plan.ItemLabel,
            changed_students = plan.ChangedStudents,
            changed_classes = plan.ChangedClasses,
            changed_sections = plan.ChangedSections,
            changed_attendance = plan.ChangedAttendance
        });
    }

    [HttpGet("parents-sync-progress/{sessionId}")]
    public IActionResult ParentsSyncProgress(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId) || !ParentsSyncProgressBySession.TryGetValue(sessionId, out var progress))
        {
            return Ok(new ParentsSyncProgressState
            {
                SessionId = sessionId,
                Status = "waiting",
                Message = "بانتظار بدء المزامنة"
            });
        }

        return Ok(progress);
    }

    [HttpPost("publish-to-parents")]
    public async Task<IActionResult> PublishToParents([FromBody] ParentsSyncRequest? request, CancellationToken cancellationToken)
    {
        var sessionId = string.IsNullOrWhiteSpace(request?.SessionId) ? Guid.NewGuid().ToString("N") : request.SessionId.Trim();
        var supabaseUrl = _config["Supabase:Url"];
        var supabaseKey = _config["Supabase:ServiceRoleKey"];

        if (string.IsNullOrWhiteSpace(supabaseUrl) || string.IsNullOrWhiteSpace(supabaseKey) || supabaseUrl.Contains("YOUR_SUPABASE"))
        {
            SetProgress(sessionId, 0, 0, "failed", "لم يتم إعداد مفاتيح Supabase في ملف appsettings.json بشكل صحيح.", true, true);
            return BadRequest(new { message = "لم يتم إعداد مفاتيح Supabase في ملف appsettings.json بشكل صحيح.", session_id = sessionId });
        }

        // إعداد الهيدر للاتصال بـ Supabase
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("apikey", supabaseKey);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", supabaseKey);
        // لعمل Upsert (تحديث إذا كان موجوداً وإضافة إذا لم يكن)
        _httpClient.DefaultRequestHeaders.Add("Prefer", "resolution=merge-duplicates");

        try
        {
            var plan = await BuildParentsSyncPlan(cancellationToken);

            if (!plan.HasChanges)
            {
                SetProgress(sessionId, 0, 0, "completed", "لا توجد تعديلات جديدة للمزامنة.", true);
                return Ok(new { message = "لا توجد تعديلات جديدة للمزامنة.", count = 0, session_id = sessionId });
            }

            // 1. جلب بيانات الطلاب من قاعدة البيانات المحلية عند الحاجة فقط
            var studentQuery = _db.StudentRecords.Where(s => s.Status == "active");
            if (plan.StudentsSince is not null)
            {
                studentQuery = studentQuery.Where(s => (s.UpdatedAt ?? s.CreatedAt ?? DateTimeOffset.MinValue) > plan.StudentsSince.Value);
            }

            var students = plan.SyncStudents
                ? await studentQuery
                .Where(s => s.Status == "active") // مزامنة الطلاب النشطين فقط
                .Select(s => new
                {
                    id = s.Id,
                    parent_phone = s.ParentPhone ?? s.Phone,
                    email = s.Email, // إضافة البريد الإلكتروني هنا
                    name = s.Name,
                    level = s.Level,
                    section = s.Section,
                    paid_amount = s.PaidAmount,
                    school_fees = s.SchoolFees,
                    uniform_fees = s.UniformFees,
                    bus_fees = s.BusFees
                })
                .ToListAsync(cancellationToken)
                : [];

            var endpoint = $"{supabaseUrl.TrimEnd('/')}/rest/v1/students_summary?on_conflict=id";
            var totalItems = Math.Max(1, plan.TotalItems);
            var uploadedItems = 0;

            if (plan.SyncStudents)
            {
                SetProgress(sessionId, totalItems, uploadedItems, "uploading_students", "جاري رفع بيانات الطلاب", itemLabel: plan.ItemLabel);

                for (var i = 0; i < students.Count; i++)
                {
                    var jsonPayload = JsonSerializer.Serialize(new[] { students[i] });
                    var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                    var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorResponse = await response.Content.ReadAsStringAsync(cancellationToken);
                        SetProgress(sessionId, totalItems, uploadedItems, "failed", "فشل الرفع إلى Supabase", true, true, errorResponse, plan.ItemLabel);
                        return StatusCode((int)response.StatusCode, new { message = "فشل الرفع إلى Supabase", details = errorResponse, session_id = sessionId });
                    }

                    uploadedItems++;
                    SetProgress(sessionId, totalItems, uploadedItems, "uploading_students", $"تم رفع {uploadedItems} من {totalItems} {plan.ItemLabel}", itemLabel: plan.ItemLabel);
                }
            }

            // 3. مزامنة الصفوف (classes)
            if (plan.SyncClasses)
            {
                SetProgress(sessionId, totalItems, uploadedItems, "syncing_classes", "جاري تحديث بيانات الصفوف", itemLabel: plan.ItemLabel);
                var classQuery = _db.GradeClasses.AsQueryable();
                if (plan.ClassesSince is not null)
                {
                    classQuery = classQuery.Where(c => (c.UpdatedAt ?? c.CreatedAt ?? DateTimeOffset.MinValue) > plan.ClassesSince.Value);
                }

                var classes = await classQuery
                .Select(c => new
                {
                    id = c.Id,
                    name = c.Name,
                    level = c.Level,
                    display_order = c.DisplayOrder,
                    tuition_fees = c.TuitionFees,
                    uniform_fees = c.UniformFees,
                    bus_fees = c.BusFees
                })
                .ToListAsync(cancellationToken);

                if (classes.Any())
                {
                    var classPayload = JsonSerializer.Serialize(classes);
                    var classContent = new StringContent(classPayload, Encoding.UTF8, "application/json");
                    var classEndpoint = $"{supabaseUrl.TrimEnd('/')}/rest/v1/classes?on_conflict=id";
                    var classResponse = await _httpClient.PostAsync(classEndpoint, classContent, cancellationToken);

                    if (!classResponse.IsSuccessStatusCode)
                    {
                        var errorResponse = await classResponse.Content.ReadAsStringAsync(cancellationToken);
                        SetProgress(sessionId, totalItems, uploadedItems, "failed", "فشل رفع بيانات الصفوف إلى Supabase", true, true, errorResponse, plan.ItemLabel);
                        return StatusCode((int)classResponse.StatusCode, new { message = "فشل رفع بيانات الصفوف إلى Supabase", details = errorResponse, session_id = sessionId });
                    }

                    uploadedItems += plan.ChangedClasses;
                    SetProgress(sessionId, totalItems, uploadedItems, "syncing_classes", $"تم تحديث الصفوف", itemLabel: plan.ItemLabel);
                }
            }

            // 3.5. مزامنة الشُعب (sections)
            if (plan.SyncSections)
            {
                SetProgress(sessionId, totalItems, uploadedItems, "syncing_sections", "جاري تحديث بيانات الشعب", itemLabel: plan.ItemLabel);
                var sectionQuery = _db.SchoolSections.AsQueryable();
                if (plan.SectionsSince is not null)
                {
                    sectionQuery = sectionQuery.Where(s => (s.UpdatedAt ?? s.CreatedAt ?? DateTimeOffset.MinValue) > plan.SectionsSince.Value);
                }

                var sections = await sectionQuery
                .Select(s => new
                {
                    id = s.Id,
                    name = s.Name,
                    class_id = s.ClassId,
                    teacher_id = s.TeacherId,
                    teacher_name = s.TeacherName
                })
                .ToListAsync(cancellationToken);

                if (sections.Any())
                {
                    var sectionPayload = JsonSerializer.Serialize(sections);
                    var sectionContent = new StringContent(sectionPayload, Encoding.UTF8, "application/json");
                    var sectionEndpoint = $"{supabaseUrl.TrimEnd('/')}/rest/v1/sections?on_conflict=id";
                    var sectionResponse = await _httpClient.PostAsync(sectionEndpoint, sectionContent, cancellationToken);

                    if (!sectionResponse.IsSuccessStatusCode)
                    {
                        var errorResponse = await sectionResponse.Content.ReadAsStringAsync(cancellationToken);
                        SetProgress(sessionId, totalItems, uploadedItems, "failed", "فشل رفع بيانات الشعب إلى Supabase", true, true, errorResponse, plan.ItemLabel);
                        return StatusCode((int)sectionResponse.StatusCode, new { message = "فشل رفع بيانات الشعب إلى Supabase", details = errorResponse, session_id = sessionId });
                    }

                    uploadedItems += plan.ChangedSections;
                    SetProgress(sessionId, totalItems, uploadedItems, "syncing_sections", "تم تحديث الشعب", itemLabel: plan.ItemLabel);
                }
            }

            // 4. مزامنة الحضور (attendance_summary) للطلاب النشطين فقط
            if (plan.SyncAttendance)
            {
                SetProgress(sessionId, totalItems, uploadedItems, "syncing_attendance", "جاري تحديث بيانات الحضور", itemLabel: plan.ItemLabel);
                var activeStudentIds = await _db.StudentRecords
                    .Where(s => s.Status == "active")
                    .Select(s => s.Id)
                    .ToListAsync(cancellationToken);

                var attendanceQuery = _db.AttendanceRecords
                    .Where(a => activeStudentIds.Contains(a.StudentId));

                if (plan.AttendanceSince is not null)
                {
                    attendanceQuery = attendanceQuery.Where(a => a.CreatedAt > plan.AttendanceSince.Value);
                }

                var attendanceRecords = await attendanceQuery
                .Select(a => new
                {
                    student_id = a.StudentId,
                    date = a.Date.ToString("yyyy-MM-dd"),
                    status = a.Status
                })
                .ToListAsync(cancellationToken);

                if (attendanceRecords.Any())
                {
                    var attEndpoint = $"{supabaseUrl.TrimEnd('/')}/rest/v1/attendance_summary?on_conflict=student_id,date";

                    foreach (var attendanceRecord in attendanceRecords)
                    {
                        var attendancePayload = JsonSerializer.Serialize(new[] { attendanceRecord });
                        var attendanceContent = new StringContent(attendancePayload, Encoding.UTF8, "application/json");
                        var attResponse = await _httpClient.PostAsync(attEndpoint, attendanceContent, cancellationToken);

                        if (!attResponse.IsSuccessStatusCode)
                        {
                            var errorResponse = await attResponse.Content.ReadAsStringAsync(cancellationToken);
                            SetProgress(sessionId, totalItems, uploadedItems, "failed", "فشل رفع بيانات الحضور إلى Supabase", true, true, errorResponse, plan.ItemLabel);
                            return StatusCode((int)attResponse.StatusCode, new { message = "فشل رفع بيانات الحضور إلى Supabase", details = errorResponse, session_id = sessionId });
                        }

                        uploadedItems++;
                        SetProgress(sessionId, totalItems, uploadedItems, "syncing_attendance", $"تم رفع {uploadedItems} من {totalItems} {plan.ItemLabel}", itemLabel: plan.ItemLabel);
                    }
                }
            }

            await SaveSuccessfulCheckpoints(plan, cancellationToken);

            SetProgress(sessionId, totalItems, totalItems, "completed", "اكتملت المزامنة", true, itemLabel: plan.ItemLabel);
            return Ok(new
            {
                message = "تمت مزامنة بيانات الطلاب والحضور بنجاح!",
                count = totalItems,
                session_id = sessionId
            });
        }
        catch (Exception ex)
        {
            SetProgress(sessionId, 0, 0, "failed", "حدث خطأ أثناء المزامنة", true, true, ex.Message);
            return StatusCode(500, new { message = "حدث خطأ أثناء المزامنة", error = ex.Message, session_id = sessionId });
        }
    }

    private static void SetProgress(
        string sessionId,
        int totalStudents,
        int uploadedStudents,
        string status,
        string message,
        bool completed = false,
        bool failed = false,
        string? error = null,
        string itemLabel = "طالب")
    {
        var safeTotal = Math.Max(0, totalStudents);
        var safeUploaded = Math.Clamp(uploadedStudents, 0, safeTotal);
        ParentsSyncProgressBySession[sessionId] = new ParentsSyncProgressState
        {
            SessionId = sessionId,
            TotalStudents = safeTotal,
            UploadedStudents = safeUploaded,
            TotalItems = safeTotal,
            UploadedItems = safeUploaded,
            ItemLabel = itemLabel,
            Percent = safeTotal == 0 ? 0 : (int)Math.Round((safeUploaded * 100m) / safeTotal),
            Status = status,
            Message = message,
            Completed = completed,
            Failed = failed,
            Error = error
        };
    }

    public sealed class ParentsSyncRequest
    {
        public string? SessionId { get; set; }
    }

    public sealed class ParentsSyncProgressState
    {
        public string? SessionId { get; set; }
        public int TotalStudents { get; set; }
        public int UploadedStudents { get; set; }
        public int TotalItems { get; set; }
        public int UploadedItems { get; set; }
        public string ItemLabel { get; set; } = "طالب";
        public int Percent { get; set; }
        public string Status { get; set; } = "waiting";
        public string Message { get; set; } = "";
        public bool Completed { get; set; }
        public bool Failed { get; set; }
        public string? Error { get; set; }
    }

    private async Task<ParentsSyncPlan> BuildParentsSyncPlan(CancellationToken cancellationToken)
    {
        await EnsureSyncCheckpointsTable(cancellationToken);

        var checkpointAt = DateTimeOffset.UtcNow;
        var checkpoints = await _db.SyncCheckpoints
            .AsNoTracking()
            .ToDictionaryAsync(x => x.Key, x => x.SyncedAt, cancellationToken);

        checkpoints.TryGetValue(StudentsCheckpointKey, out var studentsSince);
        checkpoints.TryGetValue(ClassesCheckpointKey, out var classesSince);
        checkpoints.TryGetValue(SectionsCheckpointKey, out var sectionsSince);
        checkpoints.TryGetValue(AttendanceCheckpointKey, out var attendanceSince);

        var hasStudentsCheckpoint = checkpoints.ContainsKey(StudentsCheckpointKey);
        var hasClassesCheckpoint = checkpoints.ContainsKey(ClassesCheckpointKey);
        var hasSectionsCheckpoint = checkpoints.ContainsKey(SectionsCheckpointKey);
        var hasAttendanceCheckpoint = checkpoints.ContainsKey(AttendanceCheckpointKey);

        var changedStudents = hasStudentsCheckpoint
            ? await _db.StudentRecords.CountAsync(s =>
                s.Status == "active" &&
                (s.UpdatedAt ?? s.CreatedAt ?? DateTimeOffset.MinValue) > studentsSince, cancellationToken)
            : await _db.StudentRecords.CountAsync(s => s.Status == "active", cancellationToken);

        var changedClasses = hasClassesCheckpoint
            ? await _db.GradeClasses.CountAsync(c =>
                (c.UpdatedAt ?? c.CreatedAt ?? DateTimeOffset.MinValue) > classesSince, cancellationToken)
            : await _db.GradeClasses.CountAsync(cancellationToken);

        var changedSections = hasSectionsCheckpoint
            ? await _db.SchoolSections.CountAsync(s =>
                (s.UpdatedAt ?? s.CreatedAt ?? DateTimeOffset.MinValue) > sectionsSince, cancellationToken)
            : await _db.SchoolSections.CountAsync(cancellationToken);

        var activeStudentIds = await _db.StudentRecords
            .Where(s => s.Status == "active")
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        var changedAttendance = hasAttendanceCheckpoint
            ? await _db.AttendanceRecords.CountAsync(a =>
                activeStudentIds.Contains(a.StudentId) && a.CreatedAt > attendanceSince, cancellationToken)
            : await _db.AttendanceRecords.CountAsync(a => activeStudentIds.Contains(a.StudentId), cancellationToken);

        var syncStudents = changedStudents > 0;
        var syncClasses = changedClasses > 0;
        var syncSections = changedSections > 0;
        var syncAttendance = changedAttendance > 0;
        var totalItems = changedStudents + changedClasses + changedSections + changedAttendance;
        var itemLabel = syncStudents && !syncClasses && !syncSections && !syncAttendance
            ? "طالب"
            : syncAttendance && !syncStudents && !syncClasses && !syncSections
                ? "سجل حضور"
                : "عنصر";

        return new ParentsSyncPlan
        {
            ChangedStudents = changedStudents,
            ChangedClasses = changedClasses,
            ChangedSections = changedSections,
            ChangedAttendance = changedAttendance,
            SyncStudents = syncStudents,
            SyncClasses = syncClasses,
            SyncSections = syncSections,
            SyncAttendance = syncAttendance,
            TotalItems = totalItems,
            ItemLabel = itemLabel,
            AttendanceSince = hasAttendanceCheckpoint ? attendanceSince : null,
            StudentsSince = hasStudentsCheckpoint ? studentsSince : null,
            ClassesSince = hasClassesCheckpoint ? classesSince : null,
            SectionsSince = hasSectionsCheckpoint ? sectionsSince : null,
            CheckpointAt = checkpointAt
        };
    }

    private async Task SaveSuccessfulCheckpoints(ParentsSyncPlan plan, CancellationToken cancellationToken)
    {
        if (plan.SyncStudents)
        {
            await UpsertCheckpoint(StudentsCheckpointKey, plan.CheckpointAt, cancellationToken);
        }

        if (plan.SyncClasses)
        {
            await UpsertCheckpoint(ClassesCheckpointKey, plan.CheckpointAt, cancellationToken);
        }

        if (plan.SyncSections)
        {
            await UpsertCheckpoint(SectionsCheckpointKey, plan.CheckpointAt, cancellationToken);
        }

        if (plan.SyncAttendance)
        {
            await UpsertCheckpoint(AttendanceCheckpointKey, plan.CheckpointAt, cancellationToken);
        }
    }

    private async Task UpsertCheckpoint(string key, DateTimeOffset syncedAt, CancellationToken cancellationToken)
    {
        await EnsureSyncCheckpointsTable(cancellationToken);

        var row = await _db.SyncCheckpoints.FirstOrDefaultAsync(x => x.Key == key, cancellationToken);
        if (row is null)
        {
            _db.SyncCheckpoints.Add(new Models.School.SyncCheckpointRecord
            {
                Key = key,
                SyncedAt = syncedAt
            });
        }
        else
        {
            row.SyncedAt = syncedAt;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureSyncCheckpointsTable(CancellationToken cancellationToken)
    {
        await _db.Database.ExecuteSqlRawAsync(
            """
IF OBJECT_ID(N'dbo.sync_checkpoints', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[sync_checkpoints] (
        [Key] nvarchar(120) NOT NULL,
        [synced_at] datetimeoffset NOT NULL,
        CONSTRAINT [PK_sync_checkpoints] PRIMARY KEY ([Key])
    );
END
""",
            cancellationToken);
    }

    private sealed class ParentsSyncPlan
    {
        public int ChangedStudents { get; set; }
        public int ChangedClasses { get; set; }
        public int ChangedSections { get; set; }
        public int ChangedAttendance { get; set; }
        public bool SyncStudents { get; set; }
        public bool SyncClasses { get; set; }
        public bool SyncSections { get; set; }
        public bool SyncAttendance { get; set; }
        public int TotalItems { get; set; }
        public string ItemLabel { get; set; } = "عنصر";
        public DateTimeOffset? AttendanceSince { get; set; }
        public DateTimeOffset? StudentsSince { get; set; }
        public DateTimeOffset? ClassesSince { get; set; }
        public DateTimeOffset? SectionsSince { get; set; }
        public DateTimeOffset CheckpointAt { get; set; }
        public bool HasChanges => TotalItems > 0;
    }
}
