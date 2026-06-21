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
    private const string ScheduleCheckpointKey = "parents.schedule";
    private const string InstallmentsCheckpointKey = "parents.installments";

    private static readonly ConcurrentDictionary<string, ParentsSyncProgressState> ParentsSyncProgressBySession = new();

    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _config;
    private readonly ParentsRemoteSyncPublisher _remotePublisher;
    private readonly ParentsAppIngestService _ingestService;
    private readonly BusRemoteSyncPublisher _busPublisher;
    private readonly BusAppIngestService _busIngestService;

    public SyncController(
        ApplicationDbContext db,
        IConfiguration config,
        ParentsRemoteSyncPublisher remotePublisher,
        ParentsAppIngestService ingestService,
        BusRemoteSyncPublisher busPublisher,
        BusAppIngestService busIngestService)
    {
        _db = db;
        _config = config;
        _remotePublisher = remotePublisher;
        _ingestService = ingestService;
        _busPublisher = busPublisher;
        _busIngestService = busIngestService;
    }

    /// <summary>عدد الطلاب النشطين المعروض في واجهة المزامنة (شريط التقدّم).</summary>
    [HttpGet("parents-sync-preview")]
    public async Task<IActionResult> ParentsSyncPreview(
        [FromQuery] bool force = false,
        CancellationToken cancellationToken = default)
    {
        var plan = await BuildParentsSyncPlan(cancellationToken, force);
        return Ok(new
        {
            student_count = plan.TotalItems,
            total_items = plan.TotalItems,
            item_label = plan.ItemLabel,
            changed_students = plan.ChangedStudents,
            changed_classes = plan.ChangedClasses,
            changed_sections = plan.ChangedSections,
            changed_attendance = plan.ChangedAttendance,
            changed_installments = plan.ChangedInstallments,
            changed_schedule = plan.ChangedSchedule
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

    /// <summary>عدد السجلات المحفوظة على سيرفر رويال (للتحقق بعد الرفع من المدرسة).</summary>
    [HttpGet("parents-data-status")]
    public async Task<IActionResult> ParentsDataStatus(CancellationToken cancellationToken)
    {
        if (!ValidateParentsSyncKey())
        {
            return Unauthorized(new { message = "مفتاح مزامنة رويال غير صالح." });
        }

        try
        {
            await ParentsAppTablesBootstrap.EnsureExistsAsync(_db, cancellationToken);
            await ParentsGradesTablesBootstrap.EnsureExistsAsync(_db, cancellationToken);
            var counts = new ParentsRemoteDataCounts
            {
                Students = await _db.ParentsStudentSummaries.CountAsync(cancellationToken),
                Classes = await _db.ParentsClassPublishes.CountAsync(cancellationToken),
                Sections = await _db.ParentsSectionPublishes.CountAsync(cancellationToken),
                Attendance = await _db.ParentsAttendanceSummaries.CountAsync(cancellationToken),
                StudentReports = await _db.ParentsStudentReports.CountAsync(cancellationToken),
                Installments = await _db.ParentsStudentInstallments.CountAsync(cancellationToken),
                SchedulePeriods = await _db.ParentsSchedulePeriods.CountAsync(cancellationToken),
                Grades = await _db.ParentsGradePublishes.CountAsync(cancellationToken),
                Subjects = await _db.ParentsSubjectPublishes.CountAsync(cancellationToken),
                Exams = await _db.ParentsExamPublishes.CountAsync(cancellationToken)
            };
            return Ok(counts);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "تعذر قراءة بيانات رويال", error = ex.Message });
        }
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

    [HttpGet("bus-sync-preview")]
    public async Task<IActionResult> BusSyncPreview(CancellationToken cancellationToken)
    {
        var drivers = await _db.BusPortalUsers.CountAsync(cancellationToken);
        var students = await _db.StudentRecords.CountAsync(s => s.BusDriverId != null, cancellationToken);
        return Ok(new
        {
            driver_count = drivers,
            student_count = students,
            total_items = drivers + students,
            item_label = "سجل"
        });
    }

    [HttpPost("ingest-bus")]
    public async Task<IActionResult> IngestBus([FromBody] BusSyncIngestPayload payload, CancellationToken cancellationToken)
    {
        if (!ValidateBusSyncKey())
        {
            return Unauthorized(new { message = "مفتاح مزامنة الباصات غير صالح." });
        }

        try
        {
            var result = await _busIngestService.IngestAsync(payload, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "فشل استلام بيانات الباصات", error = ex.Message });
        }
    }

    [HttpPost("publish-to-bus")]
    public async Task<IActionResult> PublishToBus(CancellationToken cancellationToken)
    {
        if (!_busPublisher.IsConfigured())
        {
            const string configMsg =
                "لم يُضبط سيرفر الباصات. أضف BusRoyal:RemoteApiUrl و BusRoyal:SyncApiKey في appsettings.Secrets.json.";
            return BadRequest(new { message = configMsg });
        }

        try
        {
            var result = await _busPublisher.PublishAsync(cancellationToken);
            return Ok(new
            {
                success = true,
                message = "تم رفع بيانات الباصات بنجاح",
                uploaded = result
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "فشل رفع بيانات الباصات", error = ex.Message });
        }
    }

    [HttpGet("bus-data-status")]
    public async Task<IActionResult> BusDataStatus(CancellationToken cancellationToken)
    {
        if (!ValidateBusSyncKey())
        {
            return Unauthorized(new { message = "مفتاح مزامنة الباصات غير صالح." });
        }

        await BusAppTablesBootstrap.EnsureExistsAsync(_db, cancellationToken);
        return Ok(new
        {
            drivers = await _db.BusAppDrivers.CountAsync(cancellationToken),
            students = await _db.BusAppStudents.CountAsync(cancellationToken),
            locations = await _db.BusAppLocations.CountAsync(cancellationToken)
        });
    }

    [HttpPost("publish-to-parents")]
    public async Task<IActionResult> PublishToParents([FromBody] ParentsSyncRequest? request, CancellationToken cancellationToken)
    {
        var sessionId = string.IsNullOrWhiteSpace(request?.SessionId) ? Guid.NewGuid().ToString("N") : request.SessionId.Trim();

        if (!_remotePublisher.IsConfigured())
        {
            const string configMsg =
                "لم يُضبط السيرفر الخارجي. أضف ParentsRoyal:RemoteApiUrl و ParentsRoyal:SyncApiKey في appsettings.Secrets.json على جهاز المدرسة.";
            SetProgress(sessionId, 0, 0, "failed", configMsg, true, true);
            return BadRequest(new { message = configMsg, session_id = sessionId });
        }

        try
        {
            var forceFull = request?.Force == true;
            var plan = await BuildParentsSyncPlan(cancellationToken, forceFull);

            if (!plan.HasChanges)
            {
                var noChangesMsg = forceFull
                    ? "لا يوجد طلاب نشطون أو بيانات للرفع."
                    : "لا توجد تعديلات جديدة للمزامنة. إذا أفرغت قاعدة التطبيق استخدم force=true لإعادة رفع الكل.";
                SetProgress(sessionId, 0, 0, "completed", noChangesMsg, true);
                return Ok(new { message = noChangesMsg, count = 0, session_id = sessionId, force = forceFull });
            }

            var totalItems = Math.Max(1, plan.TotalItems);
            SetProgress(sessionId, totalItems, 0, "uploading", "جاري الرفع إلى السيرفر الخارجي", itemLabel: plan.ItemLabel);

            var uploaded = await _remotePublisher.PublishAsync(
                plan,
                (uploadedCount, total, message) =>
                {
                    SetProgress(sessionId, total, uploadedCount, "uploading", message, itemLabel: plan.ItemLabel);
                },
                cancellationToken);

            SetProgress(sessionId, totalItems, totalItems, "uploading", "جاري التحقق من وصول البيانات إلى السيرفر الخارجي", itemLabel: plan.ItemLabel);

            ParentsRemoteDataCounts? remoteCounts;
            try
            {
                remoteCounts = await _remotePublisher.FetchRemoteCountsAsync(cancellationToken);
            }
            catch (Exception verifyEx)
            {
                remoteCounts = null;
                var outcome = ParentsSyncVerification.Evaluate(plan, uploaded, null);
                outcome.Success = false;
                outcome.FailureReason = $"تعذر التحقق من السيرفر الخارجي: {verifyEx.Message}";
                outcome.Message = "فشل التحقق بعد الرفع.";
                SetProgress(sessionId, totalItems, totalItems, "failed", outcome.FailureReason, true, true, outcome.FailureReason);
                return BadRequest(new
                {
                    success = false,
                    message = outcome.Message,
                    failure_reason = outcome.FailureReason,
                    uploaded,
                    remote = remoteCounts,
                    session_id = sessionId
                });
            }

            var result = ParentsSyncVerification.Evaluate(plan, uploaded, remoteCounts);
            if (!result.Success)
            {
                SetProgress(sessionId, totalItems, totalItems, "failed", result.FailureReason ?? result.Message, true, true, result.FailureReason);
                return BadRequest(new
                {
                    success = false,
                    message = result.Message,
                    failure_reason = result.FailureReason,
                    uploaded = result.Uploaded,
                    remote = result.Remote,
                    count = totalItems,
                    session_id = sessionId,
                    remote_url = _remotePublisher.GetRemoteSettings().RemoteUrl
                });
            }

            await SaveSuccessfulCheckpoints(plan, cancellationToken);

            SetProgress(sessionId, totalItems, totalItems, "completed", result.Message, true, itemLabel: plan.ItemLabel);
            return Ok(new
            {
                success = true,
                message = result.Message,
                count = totalItems,
                uploaded = result.Uploaded,
                remote = result.Remote,
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

    private bool ValidateBusSyncKey()
    {
        var expected = _config["BusRoyal:SyncApiKey"]?.Trim();
        if (string.IsNullOrWhiteSpace(expected))
        {
            return false;
        }

        var provided = Request.Headers["X-Bus-Sync-Key"].FirstOrDefault()?.Trim();
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

        /// <summary>تجاهل نقاط التفتيش ورفع كل الطلاب النشطين والصفوف والشعب والحضور (بعد تفريغ رويال).</summary>
        public bool Force { get; set; }
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

    private async Task<ParentsSyncPlan> BuildParentsSyncPlan(
        CancellationToken cancellationToken,
        bool forceFullSync = false)
    {
        await EnsureSyncCheckpointsTable(cancellationToken);

        var checkpointAt = DateTimeOffset.UtcNow;
        Dictionary<string, DateTimeOffset> checkpoints;
        if (forceFullSync)
        {
            checkpoints = new Dictionary<string, DateTimeOffset>(StringComparer.Ordinal);
        }
        else
        {
            checkpoints = await _db.SyncCheckpoints
                .AsNoTracking()
                .ToDictionaryAsync(x => x.Key, x => x.SyncedAt, cancellationToken);
        }

        checkpoints.TryGetValue(StudentsCheckpointKey, out var studentsSince);
        checkpoints.TryGetValue(ClassesCheckpointKey, out var classesSince);
        checkpoints.TryGetValue(SectionsCheckpointKey, out var sectionsSince);
        checkpoints.TryGetValue(AttendanceCheckpointKey, out var attendanceSince);
        checkpoints.TryGetValue(ScheduleCheckpointKey, out var scheduleSince);
        checkpoints.TryGetValue(InstallmentsCheckpointKey, out var installmentsSince);

        var hasStudentsCheckpoint = !forceFullSync && checkpoints.ContainsKey(StudentsCheckpointKey);
        var hasClassesCheckpoint = !forceFullSync && checkpoints.ContainsKey(ClassesCheckpointKey);
        var hasSectionsCheckpoint = !forceFullSync && checkpoints.ContainsKey(SectionsCheckpointKey);
        var hasAttendanceCheckpoint = !forceFullSync && checkpoints.ContainsKey(AttendanceCheckpointKey);
        var hasScheduleCheckpoint = !forceFullSync && checkpoints.ContainsKey(ScheduleCheckpointKey);
        var hasInstallmentsCheckpoint = !forceFullSync && checkpoints.ContainsKey(InstallmentsCheckpointKey);

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

        var changedSchedulePeriods = hasScheduleCheckpoint
            ? await _db.ClassSchedulePeriods.CountAsync(p => p.UpdatedAt > scheduleSince, cancellationToken)
            : await _db.ClassSchedulePeriods.CountAsync(cancellationToken);

        var changedScheduleCustom = hasScheduleCheckpoint
            ? await _db.ClassScheduleCustomItems.CountAsync(p => p.UpdatedAt > scheduleSince, cancellationToken)
            : await _db.ClassScheduleCustomItems.CountAsync(cancellationToken);

        var changedSchedule = changedSchedulePeriods + changedScheduleCustom;

        if (hasScheduleCheckpoint)
        {
            var settingsUpdated = await _db.ClassScheduleSettings
                .AsNoTracking()
                .AnyAsync(s => s.Id == 1 && s.UpdatedAt > scheduleSince, cancellationToken);
            if (settingsUpdated && changedSchedule == 0)
            {
                changedSchedule = 1;
            }
        }

        var syncStudents = changedStudents > 0;
        var syncClasses = changedClasses > 0;
        var syncSections = changedSections > 0;
        var syncAttendance = changedAttendance > 0;
        var syncSchedule = changedSchedule > 0;
        var totalItems = changedStudents + changedClasses + changedSections + changedAttendance + changedSchedule;
        var itemLabel = syncStudents && !syncClasses && !syncSections && !syncAttendance && !syncSchedule
            ? "طالب"
            : syncAttendance && !syncStudents && !syncClasses && !syncSections && !syncSchedule
                ? "سجل حضور"
                : syncSchedule && !syncStudents && !syncClasses && !syncSections && !syncAttendance
                    ? "حصة"
                    : "عنصر";

        return new ParentsSyncPlan
        {
            ChangedStudents = changedStudents,
            ChangedClasses = changedClasses,
            ChangedSections = changedSections,
            ChangedAttendance = changedAttendance,
            ChangedInstallments = changedStudents,
            ChangedSchedule = changedSchedule,
            SyncStudents = syncStudents,
            SyncClasses = syncClasses,
            SyncSections = syncSections,
            SyncAttendance = syncAttendance,
            SyncStudentReports = syncStudents,
            SyncInstallments = syncStudents,
            ChangedStudentReports = changedStudents,
            SyncSchedule = syncSchedule,
            TotalItems = totalItems,
            ItemLabel = itemLabel,
            AttendanceSince = hasAttendanceCheckpoint ? attendanceSince : null,
            StudentsSince = hasStudentsCheckpoint ? studentsSince : null,
            ClassesSince = hasClassesCheckpoint ? classesSince : null,
            SectionsSince = hasSectionsCheckpoint ? sectionsSince : null,
            ScheduleSince = hasScheduleCheckpoint ? scheduleSince : null,
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

        if (plan.SyncSchedule)
        {
            await UpsertCheckpoint(ScheduleCheckpointKey, plan.CheckpointAt, cancellationToken);
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
