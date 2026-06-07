using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolsManagement.Api.Models.School;

[Table("parents_schedule_periods")]
public class ParentsSchedulePeriodRecord
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("class_id")]
    public Guid ClassId { get; set; }

    [Column("section_id")]
    public Guid SectionId { get; set; }

    [Column("section_name")]
    [MaxLength(300)]
    public string? SectionName { get; set; }

    [Column("day_name")]
    [MaxLength(50)]
    public string DayName { get; set; } = string.Empty;

    [Column("schedule_date", TypeName = "date")]
    public DateOnly ScheduleDate { get; set; }

    [Column("period_number")]
    public int PeriodNumber { get; set; }

    [Column("subject_id")]
    public Guid? SubjectId { get; set; }

    [Column("subject_name")]
    [MaxLength(300)]
    public string? SubjectName { get; set; }

    [Column("duration_minutes")]
    public int DurationMinutes { get; set; } = 45;

    [Column("start_hour")]
    public int? StartHour { get; set; }

    [Column("start_minute")]
    public int? StartMinute { get; set; }

    [Column("end_hour")]
    public int? EndHour { get; set; }

    [Column("end_minute")]
    public int? EndMinute { get; set; }

    [Column("synced_at")]
    public DateTimeOffset SyncedAt { get; set; }
}
