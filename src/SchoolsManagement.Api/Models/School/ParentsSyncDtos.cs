namespace SchoolsManagement.Api.Models.School;

public class ParentsSyncIngestPayload
{
    public string? SchoolId { get; set; }
    public List<ParentsStudentIngestDto>? Students { get; set; }
    public List<ParentsClassIngestDto>? Classes { get; set; }
    public List<ParentsSectionIngestDto>? Sections { get; set; }
    public List<ParentsAttendanceIngestDto>? Attendance { get; set; }
    public List<ParentsStudentReportIngestDto>? StudentReports { get; set; }
    public List<ParentsInstallmentIngestDto>? Installments { get; set; }
    public List<ParentsSchedulePeriodIngestDto>? SchedulePeriods { get; set; }
    public bool ScheduleFullReplace { get; set; }
    public ParentsScheduleSettingsIngestDto? ScheduleSettings { get; set; }
    public List<ParentsGradeIngestDto>? Grades { get; set; }
    public List<ParentsSubjectIngestDto>? Subjects { get; set; }
    public List<ParentsExamIngestDto>? Exams { get; set; }
    public bool GradesReferenceFullReplace { get; set; }
}

public class ParentsStudentReportIngestDto
{
    public Guid StudentId { get; set; }
    public string? ParentPhone { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string Section { get; set; } = string.Empty;
    public decimal SchoolFees { get; set; }
    public decimal UniformFees { get; set; }
    public decimal BooksFees { get; set; }
    public decimal BusFees { get; set; }
    public decimal PaidSchoolFees { get; set; }
    public decimal PaidUniformFees { get; set; }
    public decimal PaidBooksFees { get; set; }
    public decimal PaidBusFees { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidCashAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal RemainingAmount { get; set; }
}

public class ParentsStudentIngestDto
{
    public Guid Id { get; set; }
    public string? ParentPhone { get; set; }
    public string? Email { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string Section { get; set; } = string.Empty;
    public decimal PaidAmount { get; set; }
    public decimal SchoolFees { get; set; }
    public decimal UniformFees { get; set; }
    public decimal BusFees { get; set; }
    public decimal BooksFees { get; set; }
}

public class ParentsClassIngestDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public decimal TuitionFees { get; set; }
    public decimal UniformFees { get; set; }
    public decimal BusFees { get; set; }
    public decimal BooksFees { get; set; }
}

public class ParentsSectionIngestDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid ClassId { get; set; }
    public Guid? TeacherId { get; set; }
    public string? TeacherName { get; set; }
}

public class ParentsAttendanceIngestDto
{
    public Guid StudentId { get; set; }
    public string Date { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class ParentsInstallmentIngestDto
{
    public Guid StudentId { get; set; }
    public string FeeKind { get; set; } = string.Empty;
    public int SlotIndex { get; set; }
    public string Label { get; set; } = string.Empty;
    public decimal Due { get; set; }
    public decimal Paid { get; set; }
    public decimal Remaining { get; set; }
    public bool IsFullyPaid { get; set; }
}

public class ParentsSchedulePeriodIngestDto
{
    public Guid Id { get; set; }
    public Guid ClassId { get; set; }
    public Guid SectionId { get; set; }
    public string? SectionName { get; set; }
    public string DayName { get; set; } = string.Empty;
    public string ScheduleDate { get; set; } = string.Empty;
    public int PeriodNumber { get; set; }
    public string EntryKind { get; set; } = "period";
    public string? ItemName { get; set; }
    public Guid? SubjectId { get; set; }
    public string? SubjectName { get; set; }
    public int DurationMinutes { get; set; }
    public int? StartHour { get; set; }
    public int? StartMinute { get; set; }
    public int? EndHour { get; set; }
    public int? EndMinute { get; set; }
}

public class ParentsScheduleSettingsIngestDto
{
    public string DayName { get; set; } = "الأحد";
    public int PeriodsCount { get; set; } = 6;
}

public class ParentsGradeIngestDto
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Guid SubjectId { get; set; }
    public string? SubjectName { get; set; }
    public Guid? ExamId { get; set; }
    public string? ExamType { get; set; }
    public string? ExamName { get; set; }
    public decimal Score { get; set; }
    public decimal MaxScore { get; set; }
    public decimal? Percentage { get; set; }
    public string? ExamDate { get; set; }
    public int AcademicYear { get; set; }
    public string Semester { get; set; } = "first";
    public string? Notes { get; set; }
}

public class ParentsGradeRuleIngestDto
{
    public Guid Id { get; set; }
    public decimal MinPercentage { get; set; }
    public decimal MaxPercentage { get; set; }
    public string GradeLetter { get; set; } = string.Empty;
    public string? GradeLabel { get; set; }
    public int DisplayOrder { get; set; }
}

public class ParentsSubjectIngestDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? ClassId { get; set; }
    public string? ClassName { get; set; }
    public decimal MaxScore { get; set; } = 100;
}

public class ParentsExamIngestDto
{
    public Guid Id { get; set; }
    public Guid SubjectId { get; set; }
    public string? SubjectName { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ExamType { get; set; } = "exam";
    public decimal MaxScore { get; set; }
    public string? ExamDate { get; set; }
    public int AcademicYear { get; set; }
    public string Semester { get; set; } = "first";
    public string? MonthKey { get; set; }
}

public class ParentsIngestResult
{
    public int Students { get; set; }
    public int Classes { get; set; }
    public int Sections { get; set; }
    public int Attendance { get; set; }
    public int StudentReports { get; set; }
    public int Installments { get; set; }
    public int SchedulePeriods { get; set; }
    public int ScheduleSettings { get; set; }
    public int Grades { get; set; }
    public int Subjects { get; set; }
    public int Exams { get; set; }

    public int Total => Students + Classes + Sections + Attendance + StudentReports
                        + Installments + SchedulePeriods + ScheduleSettings
                        + Grades + Subjects + Exams;
}

public class ParentsRemoteDataCounts
{
    public int Students { get; set; }
    public int Classes { get; set; }
    public int Sections { get; set; }
    public int Attendance { get; set; }
    public int StudentReports { get; set; }
    public int Installments { get; set; }
    public int SchedulePeriods { get; set; }
    public int Grades { get; set; }
    public int Subjects { get; set; }
    public int Exams { get; set; }
}

/// <summary>نتيجة رفع + تحقق من وجود البيانات على سيرفر رويال (للعرض في الواجهة).</summary>
public class ParentsPublishOutcome
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? FailureReason { get; set; }
    public ParentsIngestResult Uploaded { get; set; } = new();
    public ParentsRemoteDataCounts? Remote { get; set; }
}
