using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolsManagement.Api.Models.School;

[Table("class_schedule_settings")]
public class ClassScheduleSettingsRecord
{
    [Key]
    [Column("id")]
    public int Id { get; set; } = 1;

    [Column("day_name")]
    [MaxLength(50)]
    public string DayName { get; set; } = "الأحد";

    [Column("periods_count")]
    public int PeriodsCount { get; set; } = 6;

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }
}
