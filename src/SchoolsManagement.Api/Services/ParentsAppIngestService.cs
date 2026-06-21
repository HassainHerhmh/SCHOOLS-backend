using Microsoft.EntityFrameworkCore;
using SchoolsManagement.Api.Data;
using SchoolsManagement.Api.Models.School;

namespace SchoolsManagement.Api.Services;

/// <summary>كتابة بيانات تطبيق الآباء في جداول SQL على سيرفر رويال (الاستقبال من المدرسة).</summary>
public class ParentsAppIngestService
{
    private readonly ApplicationDbContext _db;

    public ParentsAppIngestService(ApplicationDbContext db) => _db = db;

    public async Task<ParentsIngestResult> IngestAsync(ParentsSyncIngestPayload payload, CancellationToken cancellationToken = default)
    {
        await ParentsAppTablesBootstrap.EnsureExistsAsync(_db, cancellationToken);
        var syncedAt = DateTimeOffset.UtcNow;
        var result = new ParentsIngestResult();

        if (payload.Students is { Count: > 0 })
        {
            foreach (var s in payload.Students)
            {
                var row = await _db.ParentsStudentSummaries.FirstOrDefaultAsync(x => x.Id == s.Id, cancellationToken);
                if (row is null)
                {
                    row = new ParentsStudentSummaryRecord { Id = s.Id };
                    _db.ParentsStudentSummaries.Add(row);
                }

                row.ParentPhone = s.ParentPhone;
                row.Email = s.Email;
                row.Name = s.Name;
                row.Level = s.Level;
                row.Section = s.Section;
                row.PaidAmount = s.PaidAmount;
                row.SchoolFees = s.SchoolFees;
                row.UniformFees = s.UniformFees;
                row.BusFees = s.BusFees;
                row.BooksFees = s.BooksFees;
                row.SyncedAt = syncedAt;
                result.Students++;
            }

            await _db.SaveChangesAsync(cancellationToken);
        }

        if (payload.StudentReports is { Count: > 0 })
        {
            foreach (var r in payload.StudentReports)
            {
                var row = await _db.ParentsStudentReports.FirstOrDefaultAsync(x => x.StudentId == r.StudentId, cancellationToken);
                if (row is null)
                {
                    row = new ParentsStudentReportRecord { StudentId = r.StudentId };
                    _db.ParentsStudentReports.Add(row);
                }

                row.ParentPhone = r.ParentPhone;
                row.Name = r.Name;
                row.Level = r.Level;
                row.Section = r.Section;
                row.SchoolFees = r.SchoolFees;
                row.UniformFees = r.UniformFees;
                row.BooksFees = r.BooksFees;
                row.BusFees = r.BusFees;
                row.PaidSchoolFees = r.PaidSchoolFees;
                row.PaidUniformFees = r.PaidUniformFees;
                row.PaidBooksFees = r.PaidBooksFees;
                row.PaidBusFees = r.PaidBusFees;
                row.TotalAmount = r.TotalAmount;
                row.PaidCashAmount = r.PaidCashAmount;
                row.DiscountAmount = r.DiscountAmount;
                row.RemainingAmount = r.RemainingAmount;
                row.SyncedAt = syncedAt;
                result.StudentReports++;
            }

            await _db.SaveChangesAsync(cancellationToken);
        }

        if (payload.Classes is { Count: > 0 })
        {
            foreach (var c in payload.Classes)
            {
                var row = await _db.ParentsClassPublishes.FirstOrDefaultAsync(x => x.Id == c.Id, cancellationToken);
                if (row is null)
                {
                    row = new ParentsClassPublishRecord { Id = c.Id };
                    _db.ParentsClassPublishes.Add(row);
                }

                row.Name = c.Name;
                row.Level = c.Level;
                row.DisplayOrder = c.DisplayOrder;
                row.TuitionFees = c.TuitionFees;
                row.UniformFees = c.UniformFees;
                row.BusFees = c.BusFees;
                row.BooksFees = c.BooksFees;
                row.SyncedAt = syncedAt;
                result.Classes++;
            }

            await _db.SaveChangesAsync(cancellationToken);
        }

        if (payload.Sections is { Count: > 0 })
        {
            foreach (var s in payload.Sections)
            {
                var row = await _db.ParentsSectionPublishes.FirstOrDefaultAsync(x => x.Id == s.Id, cancellationToken);
                if (row is null)
                {
                    row = new ParentsSectionPublishRecord { Id = s.Id };
                    _db.ParentsSectionPublishes.Add(row);
                }

                row.Name = s.Name;
                row.ClassId = s.ClassId;
                row.TeacherId = s.TeacherId;
                row.TeacherName = s.TeacherName;
                row.SyncedAt = syncedAt;
                result.Sections++;
            }

            await _db.SaveChangesAsync(cancellationToken);
        }

        if (payload.Attendance is { Count: > 0 })
        {
            foreach (var a in payload.Attendance)
            {
                if (!DateOnly.TryParse(a.Date, out var date))
                {
                    continue;
                }

                var row = await _db.ParentsAttendanceSummaries
                    .FirstOrDefaultAsync(x => x.StudentId == a.StudentId && x.Date == date, cancellationToken);
                if (row is null)
                {
                    row = new ParentsAttendanceSummaryRecord
                    {
                        StudentId = a.StudentId,
                        Date = date
                    };
                    _db.ParentsAttendanceSummaries.Add(row);
                }

                row.Status = a.Status;
                row.SyncedAt = syncedAt;
                result.Attendance++;
            }

            await _db.SaveChangesAsync(cancellationToken);
        }

        if (payload.Installments is { Count: > 0 })
        {
            var studentIds = payload.Installments.Select(i => i.StudentId).Distinct().ToList();
            var existing = await _db.ParentsStudentInstallments
                .Where(x => studentIds.Contains(x.StudentId))
                .ToListAsync(cancellationToken);
            if (existing.Count > 0)
            {
                _db.ParentsStudentInstallments.RemoveRange(existing);
                await _db.SaveChangesAsync(cancellationToken);
            }

            foreach (var i in payload.Installments)
            {
                _db.ParentsStudentInstallments.Add(new ParentsStudentInstallmentRecord
                {
                    StudentId = i.StudentId,
                    FeeKind = i.FeeKind,
                    SlotIndex = i.SlotIndex,
                    Label = i.Label,
                    Due = i.Due,
                    Paid = i.Paid,
                    Remaining = i.Remaining,
                    IsFullyPaid = i.IsFullyPaid,
                    SyncedAt = syncedAt
                });
                result.Installments++;
            }

            await _db.SaveChangesAsync(cancellationToken);
        }

        if (payload.ScheduleFullReplace)
        {
            await _db.ParentsSchedulePeriods.ExecuteDeleteAsync(cancellationToken);
        }

        if (payload.SchedulePeriods is { Count: > 0 })
        {
            foreach (var p in payload.SchedulePeriods)
            {
                if (!DateOnly.TryParse(p.ScheduleDate, out var scheduleDate))
                {
                    continue;
                }

                _db.ParentsSchedulePeriods.Add(new ParentsSchedulePeriodRecord
                {
                    Id = p.Id,
                    ClassId = p.ClassId,
                    SectionId = p.SectionId,
                    SectionName = p.SectionName,
                    DayName = p.DayName,
                    ScheduleDate = scheduleDate,
                    PeriodNumber = p.PeriodNumber,
                    EntryKind = string.IsNullOrWhiteSpace(p.EntryKind) ? "period" : p.EntryKind.Trim(),
                    ItemName = p.ItemName,
                    SubjectId = p.SubjectId,
                    SubjectName = p.SubjectName,
                    DurationMinutes = p.DurationMinutes,
                    StartHour = p.StartHour,
                    StartMinute = p.StartMinute,
                    EndHour = p.EndHour,
                    EndMinute = p.EndMinute,
                    SyncedAt = syncedAt
                });
                result.SchedulePeriods++;
            }

            await _db.SaveChangesAsync(cancellationToken);
        }
        else if (payload.ScheduleFullReplace)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        if (payload.ScheduleSettings is not null)
        {
            var settings = payload.ScheduleSettings;
            var row = await _db.ParentsScheduleSettings.FirstOrDefaultAsync(x => x.Id == 1, cancellationToken);
            if (row is null)
            {
                row = new ParentsScheduleSettingsRecord { Id = 1 };
                _db.ParentsScheduleSettings.Add(row);
            }

            row.DayName = settings.DayName;
            row.PeriodsCount = settings.PeriodsCount;
            row.SyncedAt = syncedAt;
            result.ScheduleSettings = 1;
            await _db.SaveChangesAsync(cancellationToken);
        }

        await IngestGradesAsync(_db, payload, syncedAt, result, cancellationToken);

        return result;
    }

    private static async Task IngestGradesAsync(
        ApplicationDbContext db,
        ParentsSyncIngestPayload payload,
        DateTimeOffset syncedAt,
        ParentsIngestResult result,
        CancellationToken cancellationToken)
    {
        if (payload.GradesReferenceFullReplace)
        {
            await db.ParentsSubjectPublishes.ExecuteDeleteAsync(cancellationToken);
            await db.ParentsExamPublishes.ExecuteDeleteAsync(cancellationToken);
        }

        if (payload.Subjects is { Count: > 0 })
        {
            foreach (var s in payload.Subjects)
            {
                db.ParentsSubjectPublishes.Add(new ParentsSubjectPublishRecord
                {
                    Id = s.Id,
                    Name = s.Name,
                    ClassId = s.ClassId,
                    ClassName = s.ClassName,
                    MaxScore = s.MaxScore,
                    SyncedAt = syncedAt
                });
                result.Subjects++;
            }
            await db.SaveChangesAsync(cancellationToken);
        }

        if (payload.Exams is { Count: > 0 })
        {
            foreach (var e in payload.Exams)
            {
                DateOnly? examDate = null;
                if (!string.IsNullOrWhiteSpace(e.ExamDate) && DateOnly.TryParse(e.ExamDate, out var parsed))
                {
                    examDate = parsed;
                }

                db.ParentsExamPublishes.Add(new ParentsExamPublishRecord
                {
                    Id = e.Id,
                    SubjectId = e.SubjectId,
                    SubjectName = e.SubjectName,
                    Name = e.Name,
                    ExamType = e.ExamType,
                    MaxScore = e.MaxScore,
                    ExamDate = examDate,
                    AcademicYear = e.AcademicYear,
                    Semester = e.Semester,
                    MonthKey = e.MonthKey,
                    SyncedAt = syncedAt
                });
                result.Exams++;
            }
            await db.SaveChangesAsync(cancellationToken);
        }

        if (payload.Grades is { Count: > 0 })
        {
            foreach (var g in payload.Grades)
            {
                DateOnly? examDate = null;
                if (!string.IsNullOrWhiteSpace(g.ExamDate) && DateOnly.TryParse(g.ExamDate, out var parsed))
                {
                    examDate = parsed;
                }

                var row = await db.ParentsGradePublishes.FirstOrDefaultAsync(x => x.Id == g.Id, cancellationToken);
                if (row is null)
                {
                    row = new ParentsGradePublishRecord { Id = g.Id };
                    db.ParentsGradePublishes.Add(row);
                }

                row.StudentId = g.StudentId;
                row.SubjectId = g.SubjectId;
                row.SubjectName = g.SubjectName;
                row.ExamId = g.ExamId;
                row.ExamType = g.ExamType;
                row.ExamName = g.ExamName;
                row.Score = g.Score;
                row.MaxScore = g.MaxScore;
                row.Percentage = g.Percentage;
                row.ExamDate = examDate;
                row.AcademicYear = g.AcademicYear;
                row.Semester = g.Semester;
                row.Notes = g.Notes;
                row.SyncedAt = syncedAt;
                result.Grades++;
            }

            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
