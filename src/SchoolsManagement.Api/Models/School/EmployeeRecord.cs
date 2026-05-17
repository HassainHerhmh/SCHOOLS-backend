using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolsManagement.Api.Models.School;

[Table("employees")]
public class EmployeeRecord
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(500)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(250)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(40)]
    public string? Phone { get; set; }

    [Column("password")]
    [MaxLength(500)]
    public string PasswordHash { get; set; } = string.Empty;

    [MaxLength(200)]
    public string Position { get; set; } = "موظف";

    [Column("employee_type")]
    [MaxLength(40)]
    public string EmployeeType { get; set; } = "employee";

    [MaxLength(40)]
    public string Status { get; set; } = "active";

    [MaxLength(300)]
    public string? Specialization { get; set; }

    [MaxLength(300)]
    public string? Subject { get; set; }

    [Column("base_salary", TypeName = "decimal(18,2)")]
    public decimal BaseSalary { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Allowances { get; set; }

    [Column("responsible_class_id")]
    public Guid? ResponsibleClassId { get; set; }

    /// <summary>حساب محاسبي فرعي (ورقة) في دليل الحسابات لترحيل خصومات واستحقاقات الموظف.</summary>
    [Column("chart_account_id")]
    public int? ChartAccountId { get; set; }

    [Column("is_first_login")]
    public bool IsFirstLogin { get; set; } = true;

    [Column("last_login")]
    public DateTimeOffset? LastLogin { get; set; }

    [Column("created_at")]
    public DateTimeOffset? CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTimeOffset? UpdatedAt { get; set; }
}
