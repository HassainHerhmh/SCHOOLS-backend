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

        return result;
    }
}
