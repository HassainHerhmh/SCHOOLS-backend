using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolsManagement.Api.Models.School;

/// <summary>نسخة منشورة لتطبيق أولياء الأمور (بديل Supabase students_summary).</summary>
[Table("parents_students_summary")]
public class ParentsStudentSummaryRecord
{
    [Key]
    public Guid Id { get; set; }

    [Column("parent_phone")]
    [MaxLength(40)]
    public string? ParentPhone { get; set; }

    [MaxLength(250)]
    public string? Email { get; set; }

    [MaxLength(500)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(200)]
    public string Level { get; set; } = string.Empty;

    [MaxLength(200)]
    public string Section { get; set; } = string.Empty;

    [Column("paid_amount", TypeName = "decimal(18,2)")]
    public decimal PaidAmount { get; set; }

    [Column("school_fees", TypeName = "decimal(18,2)")]
    public decimal SchoolFees { get; set; }

    [Column("uniform_fees", TypeName = "decimal(18,2)")]
    public decimal UniformFees { get; set; }

    [Column("bus_fees", TypeName = "decimal(18,2)")]
    public decimal BusFees { get; set; }

    [Column("synced_at")]
    public DateTimeOffset SyncedAt { get; set; }
}
