namespace SchoolsManagement.Api.Models.School;

public class UpsertGradeClassRequest
{
    public string Name { get; set; } = string.Empty;

    public int Level { get; set; }

    public int DisplayOrder { get; set; }

    public decimal TuitionFees { get; set; }

    public decimal UniformFees { get; set; }

    public decimal BooksFees { get; set; }

    public decimal BusFees { get; set; }
}

public class UpsertSectionRequest
{
    public string Name { get; set; } = string.Empty;

    public Guid ClassId { get; set; }

    /// <summary>معرّف المعلّم كسلسلة (من سوبر بيس أو محلي)؛ فارغ = بدون معلّم.</summary>
    public string? TeacherId { get; set; }

    public string? TeacherName { get; set; }
}

public sealed class SectionResponse
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public Guid ClassId { get; init; }

    public string ClassName { get; init; } = string.Empty;

    public string? TeacherId { get; init; }

    public string? TeacherName { get; init; }

    public DateTimeOffset? CreatedAt { get; init; }

    public DateTimeOffset? UpdatedAt { get; init; }
}
