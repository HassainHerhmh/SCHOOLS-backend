namespace SchoolsManagement.Api.Models.School;

public class ParentsSyncIngestPayload
{
    public string? SchoolId { get; set; }
    public List<ParentsStudentIngestDto>? Students { get; set; }
    public List<ParentsClassIngestDto>? Classes { get; set; }
    public List<ParentsSectionIngestDto>? Sections { get; set; }
    public List<ParentsAttendanceIngestDto>? Attendance { get; set; }
    public List<ParentsStudentReportIngestDto>? StudentReports { get; set; }
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

public class ParentsIngestResult
{
    public int Students { get; set; }
    public int Classes { get; set; }
    public int Sections { get; set; }
    public int Attendance { get; set; }
    public int StudentReports { get; set; }

    public int Total => Students + Classes + Sections + Attendance + StudentReports;
}

public class ParentsRemoteDataCounts
{
    public int Students { get; set; }
    public int Classes { get; set; }
    public int Sections { get; set; }
    public int Attendance { get; set; }
    public int StudentReports { get; set; }
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
