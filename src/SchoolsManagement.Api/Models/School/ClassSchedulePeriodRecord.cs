using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolsManagement.Api.Models.School;

[Table("class_schedule_periods")]
public class ClassSchedulePeriodRecord
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("class_id")]
    public Guid ClassId { get; set; }

    [Column("section_id")]
    public Guid SectionId { get; set; }

    [Column("day_name")]
    [MaxLength(50)]
    public string DayName { get; set; } = string.Empty;

    [Column("period_number")]
    public int PeriodNumber { get; set; }

    [Column("subject_id")]
    public Guid? SubjectId { get; set; }

    [Column("duration_minutes")]
    public int DurationMinutes { get; set; } = 45;

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }
}
