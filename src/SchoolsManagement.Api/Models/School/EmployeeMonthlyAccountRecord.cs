using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolsManagement.Api.Models.School;

[Table("employee_monthly_accounts")]
public class EmployeeMonthlyAccountRecord
{
    [Key]
    public Guid Id { get; set; }

    [Column("employee_id")]
    public Guid EmployeeId { get; set; }

    [ForeignKey(nameof(EmployeeId))]
    public EmployeeRecord? Employee { get; set; }

    [Column("employee_name")]
    [MaxLength(500)]
    public string EmployeeName { get; set; } = string.Empty;

    public int Year { get; set; }

    public int Month { get; set; }

    [Column("month_name")]
    [MaxLength(50)]
    public string MonthName { get; set; } = string.Empty;

    [Column("base_salary", TypeName = "decimal(18,2)")]
    public decimal BaseSalary { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Allowances { get; set; }

    [Column("total_deductions", TypeName = "decimal(18,2)")]
    public decimal TotalDeductions { get; set; }

    [Column("total_bonuses", TypeName = "decimal(18,2)")]
    public decimal TotalBonuses { get; set; }

    [Column("total_absence_days")]
    public int TotalAbsenceDays { get; set; }

    [Column("absence_deduction", TypeName = "decimal(18,2)")]
    public decimal AbsenceDeduction { get; set; }

    [Column("total_delay_minutes")]
    public int TotalDelayMinutes { get; set; }

    [Column("delay_deduction", TypeName = "decimal(18,2)")]
    public decimal DelayDeduction { get; set; }

    [Column("total_extra_hours", TypeName = "decimal(18,2)")]
    public decimal TotalExtraHours { get; set; }

    [Column("extra_pay", TypeName = "decimal(18,2)")]
    public decimal ExtraPay { get; set; }

    [Column("deductions_json")]
    public string DeductionsJson { get; set; } = "[]";

    [Column("bonuses_json")]
    public string BonusesJson { get; set; } = "[]";

    [Column("attendance_json")]
    public string AttendanceJson { get; set; } = "[]";

    [Column("absences_json")]
    public string AbsencesJson { get; set; } = "[]";

    [Column("delays_json")]
    public string DelaysJson { get; set; } = "[]";

    [Column("extra_hours_json")]
    public string ExtraHoursJson { get; set; } = "[]";

    [Column("gross_salary", TypeName = "decimal(18,2)")]
    public decimal GrossSalary { get; set; }

    [Column("net_salary", TypeName = "decimal(18,2)")]
    public decimal NetSalary { get; set; }

    [MaxLength(40)]
    public string Status { get; set; } = "draft";

    [Column("is_paid")]
    public bool IsPaid { get; set; }

    [Column("paid_at")]
    public DateTimeOffset? PaidAt { get; set; }

    [Column("paid_by")]
    [MaxLength(200)]
    public string? PaidBy { get; set; }

    [Column("payment_method")]
    [MaxLength(120)]
    public string? PaymentMethod { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }

    [Column("created_at")]
    public DateTimeOffset? CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTimeOffset? UpdatedAt { get; set; }
}
