using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolsManagement.Api.Models.School;

[Table("parents_attendance_summary")]
public class ParentsAttendanceSummaryRecord
{
    [Column("student_id")]
    public Guid StudentId { get; set; }

    [Column("date", TypeName = "date")]
    public DateOnly Date { get; set; }

    [MaxLength(50)]
    public string Status { get; set; } = string.Empty;

    [Column("synced_at")]
    public DateTimeOffset SyncedAt { get; set; }
}
