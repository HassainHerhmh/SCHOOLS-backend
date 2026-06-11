using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolsManagement.Api.Models.School;

[Table("exam_schedules")]
public class ExamScheduleRecord
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("class_id")]
    public Guid ClassId { get; set; }

    [Column("exam_month")]
    [MaxLength(30)]
    public string ExamMonth { get; set; } = string.Empty;

    [Column("day_name")]
    [MaxLength(50)]
    public string DayName { get; set; } = string.Empty;

    [Column("subject_id")]
    public Guid? SubjectId { get; set; }

    [Column("exam_date", TypeName = "date")]
    public DateOnly? ExamDate { get; set; }

    [Column("duration_minutes")]
    public int? DurationMinutes { get; set; }

    [Column("sort_order")]
    public int SortOrder { get; set; }

    [Column("schedule_kind")]
    [MaxLength(20)]
    public string ScheduleKind { get; set; } = "quiz";

    [MaxLength(20)]
    public string? Semester { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }
}
