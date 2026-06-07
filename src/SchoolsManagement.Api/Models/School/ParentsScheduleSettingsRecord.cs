using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolsManagement.Api.Models.School;

[Table("parents_schedule_settings")]
public class ParentsScheduleSettingsRecord
{
    [Key]
    public int Id { get; set; } = 1;

    [Column("day_name")]
    [MaxLength(50)]
    public string DayName { get; set; } = "الأحد";

    [Column("periods_count")]
    public int PeriodsCount { get; set; } = 6;

    [Column("synced_at")]
    public DateTimeOffset SyncedAt { get; set; }
}
