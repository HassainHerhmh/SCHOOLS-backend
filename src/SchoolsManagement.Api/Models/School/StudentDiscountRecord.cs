using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolsManagement.Api.Models.School;

[Table("student_discounts")]
public class StudentDiscountRecord
{
    [Key]
    public Guid Id { get; set; }

    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    public string? Description { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTimeOffset? UpdatedAt { get; set; }
}

[Table("student_discount_applications")]
public class StudentDiscountApplicationRecord
{
    [Key]
    public Guid Id { get; set; }

    [Column("student_id")]
    public Guid StudentId { get; set; }

    [Column("discount_id")]
    public Guid DiscountId { get; set; }

    [Column("discount_name")]
    [MaxLength(200)]
    public string DiscountName { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Column("applied_at")]
    public DateTimeOffset AppliedAt { get; set; }

    public string? Notes { get; set; }

    [Column("created_by")]
    [MaxLength(200)]
    public string? CreatedBy { get; set; }
}
