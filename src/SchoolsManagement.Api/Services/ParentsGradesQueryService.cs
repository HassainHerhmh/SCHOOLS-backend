using Microsoft.EntityFrameworkCore;
using SchoolsManagement.Api.Data;
using SchoolsManagement.Api.Models.School;

namespace SchoolsManagement.Api.Services;

public class ParentsGradesQueryService
{
    private readonly ApplicationDbContext _db;

    public ParentsGradesQueryService(ApplicationDbContext db) => _db = db;

    public async Task<object> GetBundleAsync(Guid studentId, int? academicYear, CancellationToken ct)
    {
        await ParentsAppTablesBootstrap.EnsureExistsAsync(_db, ct);
        await ParentsGradesTablesBootstrap.EnsureExistsAsync(_db, ct);

        var year = academicYear ?? DateTime.UtcNow.Year;
        var student = await _db.ParentsStudentSummaries.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == studentId, ct);
        if (student is null)
        {
            student = await _db.StudentRecords.AsNoTracking()
                .Where(s => s.Id == studentId)
                .Select(s => new ParentsStudentSummaryRecord
                {
                    Id = s.Id,
                    Name = s.Name,
                    Level = s.Level,
                    Section = s.Section,
                    ParentPhone = s.ParentPhone
                })
                .FirstOrDefaultAsync(ct);
        }

        if (student is null)
        {
            return new { message = "الطالب غير موجود." };
        }

        var publishedGrades = await _db.ParentsGradePublishes.AsNoTracking()
            .Where(g => g.StudentId == studentId && g.AcademicYear == year)
            .OrderBy(g => g.ExamDate)
            .ToListAsync(ct);

        IEnumerable<object> gradeRows;
        if (publishedGrades.Count > 0)
        {
            gradeRows = publishedGrades;
        }
        else
        {
            var rows = await _db.Grades.AsNoTracking()
                .Where(g => g.StudentId == studentId && g.AcademicYear == year)
                .OrderBy(g => g.ExamDate)
                .ToListAsync(ct);
            gradeRows = rows.Cast<object>();
        }

        var publishedSubjects = await _db.ParentsSubjectPublishes.AsNoTracking().ToListAsync(ct);
        var subjects = publishedSubjects.Count > 0
            ? publishedSubjects
            : await _db.Subjects.AsNoTracking()
                .Select(s => new ParentsSubjectPublishRecord
                {
                    Id = s.Id,
                    Name = s.Name,
                    ClassId = s.ClassId,
                    ClassName = null,
                    MaxScore = 100
                })
                .ToListAsync(ct);

        var publishedExams = await _db.ParentsExamPublishes.AsNoTracking()
            .Where(e => e.AcademicYear == year)
            .ToListAsync(ct);
        var exams = publishedExams.Count > 0
            ? publishedExams
            : await _db.Exams.AsNoTracking()
                .Where(e => e.AcademicYear == year || e.AcademicYear == null)
                .Select(e => new ParentsExamPublishRecord
                {
                    Id = e.Id,
                    SubjectId = e.SubjectId,
                    Name = e.Title,
                    ExamType = e.ActivityType ?? e.ScheduleKind,
                    MaxScore = e.MaxScore,
                    ExamDate = e.ExamDate,
                    AcademicYear = e.AcademicYear ?? year,
                    Semester = e.Semester,
                    MonthKey = e.ExamMonth
                })
                .ToListAsync(ct);

        return new
        {
            student = new
            {
                student.Id,
                student.Name,
                level = student.Level,
                section = student.Section,
                parent_phone = student.ParentPhone
            },
            academic_year = year,
            grades = gradeRows.Select(MapGrade),
            subjects = subjects.Select(MapSubject),
            exams = exams.Select(MapExam)
        };
    }

    private static object MapGrade(object row)
    {
        return row switch
        {
            ParentsGradePublishRecord g => new
            {
                id = g.Id,
                student_id = g.StudentId,
                subject_id = g.SubjectId,
                subject_name = g.SubjectName,
                exam_id = g.ExamId,
                exam_type = g.ExamType,
                exam_name = g.ExamName,
                score = g.Score,
                max_score = g.MaxScore,
                percentage = g.Percentage,
                exam_date = g.ExamDate?.ToString("yyyy-MM-dd"),
                academic_year = g.AcademicYear,
                semester = g.Semester,
                notes = g.Notes
            },
            GradeRecord g => new
            {
                id = g.Id,
                student_id = g.StudentId,
                subject_id = g.SubjectId,
                subject_name = g.SubjectName,
                exam_id = g.ExamId,
                exam_type = g.ExamType,
                exam_name = g.ExamName,
                score = g.Score,
                max_score = g.MaxScore,
                percentage = g.Percentage,
                exam_date = g.ExamDate?.ToString("yyyy-MM-dd"),
                academic_year = g.AcademicYear,
                semester = g.Semester,
                notes = g.Notes
            },
            _ => row
        };
    }

    private static object MapSubject(ParentsSubjectPublishRecord s) => new
    {
        id = s.Id,
        name = s.Name,
        class_id = s.ClassId,
        class_name = s.ClassName,
        max_score = s.MaxScore
    };

    private static object MapExam(ParentsExamPublishRecord e) => new
    {
        id = e.Id,
        subject_id = e.SubjectId,
        subject_name = e.SubjectName,
        name = e.Name,
        exam_type = e.ExamType,
        max_score = e.MaxScore,
        exam_date = e.ExamDate?.ToString("yyyy-MM-dd"),
        academic_year = e.AcademicYear,
        semester = e.Semester,
        month_key = e.MonthKey,
        activity_type = e.ExamType
    };
}
