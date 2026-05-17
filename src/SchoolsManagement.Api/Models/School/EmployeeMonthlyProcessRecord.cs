using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolsManagement.Api.Models.School;

[Table("employee_monthly_processes")]
public class EmployeeMonthlyProcessRecord
{
    [Key]
    public Guid Id { get; set; }

    public int Year { get; set; }

    public int Month { get; set; }

    [Column("month_name")]
    [MaxLength(50)]
    public string MonthName { get; set; } = string.Empty;

    [Column("start_date")]
    public DateTimeOffset? StartDate { get; set; }

    [Column("end_date")]
    public DateTimeOffset? EndDate { get; set; }

    [MaxLength(40)]
    public string Status { get; set; } = "processing";

    [Column("created_at")]
    public DateTimeOffset? CreatedAt { get; set; }

    [Column("completed_at")]
    public DateTimeOffset? CompletedAt { get; set; }
}
