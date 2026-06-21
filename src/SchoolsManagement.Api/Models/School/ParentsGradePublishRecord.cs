using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolsManagement.Api.Models.School;

[Table("parents_grades")]
public class ParentsGradePublishRecord
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

    [Column("synced_at")]
    public DateTimeOffset SyncedAt { get; set; }
}
