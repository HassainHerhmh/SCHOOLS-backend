using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolsManagement.Api.Models.School;

[Table("parents_subjects")]
public class ParentsSubjectPublishRecord
{
    [Key]
    public Guid Id { get; set; }

    [Column("name")]
    [MaxLength(300)]
    public string Name { get; set; } = string.Empty;

    [Column("class_id")]
    public Guid? ClassId { get; set; }

    [Column("class_name")]
    [MaxLength(200)]
    public string? ClassName { get; set; }

    [Column("max_score", TypeName = "decimal(18,2)")]
    public decimal MaxScore { get; set; } = 100;

    [Column("synced_at")]
    public DateTimeOffset SyncedAt { get; set; }
}

[Table("parents_exams")]
public class ParentsExamPublishRecord
{
    [Key]
    public Guid Id { get; set; }

    [Column("subject_id")]
    public Guid SubjectId { get; set; }

    [Column("subject_name")]
    [MaxLength(250)]
    public string? SubjectName { get; set; }

    [Column("name")]
    [MaxLength(250)]
    public string Name { get; set; } = string.Empty;

    [Column("exam_type")]
    [MaxLength(80)]
    public string ExamType { get; set; } = "exam";

    [Column("max_score", TypeName = "decimal(18,2)")]
    public decimal MaxScore { get; set; }

    [Column("exam_date")]
    public DateOnly? ExamDate { get; set; }

    [Column("academic_year")]
    public int AcademicYear { get; set; }

    [MaxLength(20)]
    public string Semester { get; set; } = "first";

    [Column("month_key")]
    [MaxLength(20)]
    public string? MonthKey { get; set; }

    [Column("synced_at")]
    public DateTimeOffset SyncedAt { get; set; }
}
