using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolsManagement.Api.Models.School;

/// <summary>تقرير مديونيات الطالب المنشور لتطبيق أولياء الأمور.</summary>
[Table("parents_student_reports")]
public class ParentsStudentReportRecord
{
    [Key]
    [Column("student_id")]
    public Guid StudentId { get; set; }

    [Column("parent_phone")]
    [MaxLength(40)]
    public string? ParentPhone { get; set; }

    [MaxLength(500)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(200)]
    public string Level { get; set; } = string.Empty;

    [MaxLength(200)]
    public string Section { get; set; } = string.Empty;

    [Column("school_fees", TypeName = "decimal(18,2)")]
    public decimal SchoolFees { get; set; }

    [Column("uniform_fees", TypeName = "decimal(18,2)")]
    public decimal UniformFees { get; set; }

    [Column("books_fees", TypeName = "decimal(18,2)")]
    public decimal BooksFees { get; set; }

    [Column("bus_fees", TypeName = "decimal(18,2)")]
    public decimal BusFees { get; set; }

    [Column("paid_school_fees", TypeName = "decimal(18,2)")]
    public decimal PaidSchoolFees { get; set; }

    [Column("paid_uniform_fees", TypeName = "decimal(18,2)")]
    public decimal PaidUniformFees { get; set; }

    [Column("paid_books_fees", TypeName = "decimal(18,2)")]
    public decimal PaidBooksFees { get; set; }

    [Column("paid_bus_fees", TypeName = "decimal(18,2)")]
    public decimal PaidBusFees { get; set; }

    [Column("total_amount", TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    [Column("paid_cash_amount", TypeName = "decimal(18,2)")]
    public decimal PaidCashAmount { get; set; }

    [Column("discount_amount", TypeName = "decimal(18,2)")]
    public decimal DiscountAmount { get; set; }

    [Column("remaining_amount", TypeName = "decimal(18,2)")]
    public decimal RemainingAmount { get; set; }

    [Column("synced_at")]
    public DateTimeOffset SyncedAt { get; set; }
}
