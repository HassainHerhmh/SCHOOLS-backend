using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolsManagement.Api.Configuration;
using SchoolsManagement.Api.Data;
using SchoolsManagement.Api.Models.School;
using SchoolsManagement.Api.Services;
using ParentsSyncPlan = SchoolsManagement.Api.Services.ParentsRemoteSyncPublisher.ParentsSyncPlan;

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
    private readonly ParentsRemoteSyncPublisher _remotePublisher;
    private readonly ParentsAppIngestService _ingestService;

    public SyncController(
        ApplicationDbContext db,
        IConfiguration config,
        ParentsRemoteSyncPublisher remotePublisher,
        ParentsAppIngestService ingestService)
    {
        _db = db;
        _config = config;
        _remotePublisher = remotePublisher;
        _ingestService = ingestService;
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

    /// <summary>استقبال البيانات على سيرفر رويال الخارجي (يُستدعى من المدرسة المحلية فقط).</summary>
    [HttpPost("ingest-parents")]
    public async Task<IActionResult> IngestParents([FromBody] ParentsSyncIngestPayload payload, CancellationToken cancellationToken)
    {
        if (!ValidateParentsSyncKey())
        {
            return Unauthorized(new { message = "مفتاح مزامنة رويال غير صالح." });
        }

        try
        {
            var result = await _ingestService.IngestAsync(payload, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "فشل استلام البيانات", error = ex.Message });
        }
    }

    [HttpPost("publish-to-parents")]
    public async Task<IActionResult> PublishToParents([FromBody] ParentsSyncRequest? request, CancellationToken cancellationToken)
    {
        var sessionId = string.IsNullOrWhiteSpace(request?.SessionId) ? Guid.NewGuid().ToString("N") : request.SessionId.Trim();

        if (!_remotePublisher.IsConfigured())
        {
            const string configMsg =
                "لم يُضبط سيرفر رويال الخارجي. أضف ParentsRoyal:RemoteApiUrl و ParentsRoyal:SyncApiKey في appsettings.Secrets.json على جهاز المدرسة.";
            SetProgress(sessionId, 0, 0, "failed", configMsg, true, true);
            return BadRequest(new { message = configMsg, session_id = sessionId });
        }

        try
        {
            var plan = await BuildParentsSyncPlan(cancellationToken);

            if (!plan.HasChanges)
            {
                SetProgress(sessionId, 0, 0, "completed", "لا توجد تعديلات جديدة للمزامنة.", true);
                return Ok(new { message = "لا توجد تعديلات جديدة للمزامنة.", count = 0, session_id = sessionId });
            }

            var totalItems = Math.Max(1, plan.TotalItems);
            SetProgress(sessionId, totalItems, 0, "uploading", "جاري الرفع إلى سيرفر رويال الخارجي", itemLabel: plan.ItemLabel);

            await _remotePublisher.PublishAsync(
                plan,
                (uploaded, total, message) =>
                {
                    SetProgress(sessionId, total, uploaded, "uploading", message, itemLabel: plan.ItemLabel);
                },
                cancellationToken);

            await SaveSuccessfulCheckpoints(plan, cancellationToken);

            SetProgress(sessionId, totalItems, totalItems, "completed", "اكتمل الرفع إلى سيرفر رويال", true, itemLabel: plan.ItemLabel);
            return Ok(new
            {
                message = "تم رفع بيانات تطبيق أولياء الأمور إلى سيرفر رويال الخارجي بنجاح.",
                count = totalItems,
                session_id = sessionId,
                remote_url = _remotePublisher.GetRemoteSettings().RemoteUrl
            });
        }
        catch (Exception ex)
        {
            var detail = BuildSyncErrorDetail(ex);
            SetProgress(sessionId, 0, 0, "failed", detail, true, true, detail);
            return StatusCode(500, new
            {
                message = detail,
                error = detail,
                detail,
                session_id = sessionId
            });
        }
    }

    private static string BuildSyncErrorDetail(Exception ex)
    {
        var parts = new List<string>();
        for (var current = ex; current != null; current = current.InnerException)
        {
            if (!string.IsNullOrWhiteSpace(current.Message)
                && !parts.Contains(current.Message, StringComparer.Ordinal))
            {
                parts.Add(current.Message);
            }
        }

        return parts.Count > 0 ? string.Join(" ← ", parts) : "حدث خطأ أثناء المزامنة";
    }

    private bool ValidateParentsSyncKey()
    {
        var expected = _config["ParentsRoyal:SyncApiKey"]?.Trim();
        if (string.IsNullOrWhiteSpace(expected))
        {
            return false;
        }

        var provided = Request.Headers["X-Parents-Sync-Key"].FirstOrDefault()?.Trim();
        return string.Equals(expected, provided, StringComparison.Ordinal);
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
        if (DatabaseProviderHelper.IsMySql(_db))
        {
            await _db.Database.ExecuteSqlRawAsync(
                """
                CREATE TABLE IF NOT EXISTS sync_checkpoints (
                    `Key` varchar(120) NOT NULL,
                    synced_at datetime(6) NOT NULL,
                    PRIMARY KEY (`Key`)
                );
                """,
                cancellationToken);
            return;
        }

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

}
