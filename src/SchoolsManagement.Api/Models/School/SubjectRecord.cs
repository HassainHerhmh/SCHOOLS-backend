using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolsManagement.Api.Models.School;

[Table("subjects")]
public class SubjectRecord
{
    [Key]
    public Guid Id { get; set; }

    [Column("class_id")]
    public Guid ClassId { get; set; }

    [MaxLength(250)]
    public string Name { get; set; } = string.Empty;

    [Column("teacher_id")]
    public Guid? TeacherId { get; set; }

    [Column("teacher_name")]
    [MaxLength(250)]
    public string? TeacherName { get; set; }

    [Column("created_at")]
    public DateTimeOffset? CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTimeOffset? UpdatedAt { get; set; }
}

[Table("exams")]
public class ExamRecord
{
    [Key]
    public Guid Id { get; set; }

    [Column("subject_id")]
    public Guid SubjectId { get; set; }

    [MaxLength(250)]
    public string Title { get; set; } = string.Empty;

    [Column("exam_date")]
    public DateOnly? ExamDate { get; set; }

    [Column("max_score", TypeName = "decimal(18,2)")]
    public decimal MaxScore { get; set; } = 100;

    [Column("exam_month")]
    [MaxLength(30)]
    public string? ExamMonth { get; set; }

    [MaxLength(20)]
    public string Semester { get; set; } = "first";

    [Column("activity_type")]
    [MaxLength(50)]
    public string? ActivityType { get; set; }

    [Column("academic_year")]
    public int? AcademicYear { get; set; }

    [Column("schedule_kind")]
    [MaxLength(20)]
    public string ScheduleKind { get; set; } = "quiz";

    [Column("created_at")]
    public DateTimeOffset? CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTimeOffset? UpdatedAt { get; set; }
}

[Table("grade_rules")]
public class GradeRuleRecord
{
    [Key]
    public Guid Id { get; set; }

    [Column("class_id")]
    public Guid ClassId { get; set; }

    [Column("subject_id")]
    public Guid SubjectId { get; set; }

    [Column("min_pass_score", TypeName = "decimal(18,2)")]
    public decimal MinPassScore { get; set; }

    [Column("created_at")]
    public DateTimeOffset? CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTimeOffset? UpdatedAt { get; set; }
}

[Table("grades")]
public class GradeRecord
{
    [Key]
    public Guid Id { get; set; }

    [Column("student_id")]
    public Guid StudentId { get; set; }

    [Column("subject_id")]
    public Guid SubjectId { get; set; }

    [Column("subject_name")]
    [MaxLength(250)]
    public string? SubjectName { get; set; }

    [Column("exam_id")]
    public Guid? ExamId { get; set; }

    [Column("exam_type")]
    [MaxLength(80)]
    public string? ExamType { get; set; }

    [Column("exam_name")]
    [MaxLength(250)]
    public string? ExamName { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Score { get; set; }

    [Column("max_score", TypeName = "decimal(18,2)")]
    public decimal MaxScore { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? Percentage { get; set; }

    [Column("exam_date")]
    public DateOnly? ExamDate { get; set; }

    [Column("academic_year")]
    public int AcademicYear { get; set; }

    [MaxLength(20)]
    public string Semester { get; set; } = "first";

    public string? Notes { get; set; }

    [Column("created_by")]
    [MaxLength(200)]
    public string? CreatedBy { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTimeOffset? UpdatedAt { get; set; }
}
