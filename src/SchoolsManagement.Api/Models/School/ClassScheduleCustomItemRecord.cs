using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolsManagement.Api.Models.School;

[Table("class_schedule_custom_items")]
public class ClassScheduleCustomItemRecord
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

    [Column("schedule_date", TypeName = "date")]
    public DateOnly ScheduleDate { get; set; }

    [Column("item_name")]
    [MaxLength(200)]
    public string ItemName { get; set; } = string.Empty;

    [Column("position_number")]
    public int PositionNumber { get; set; }

    [Column("start_hour")]
    public int StartHour { get; set; }

    [Column("start_minute")]
    public int StartMinute { get; set; }

    [Column("end_hour")]
    public int EndHour { get; set; }

    [Column("end_minute")]
    public int EndMinute { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }
}
