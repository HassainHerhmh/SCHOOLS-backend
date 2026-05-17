using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolsManagement.Api.Models.School;

[Table("student_payments")]
public class StudentPaymentRecord
{
    [Key]
    public Guid Id { get; set; }

    [Column("student_id")]
    public Guid StudentId { get; set; }

    [Column("student_name")]
    [MaxLength(300)]
    public string? StudentName { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Column("payment_date")]
    public DateOnly PaymentDate { get; set; }

    [Column("receipt_number")]
    [MaxLength(80)]
    public string ReceiptNumber { get; set; } = string.Empty;

    [Column("school_fees_paid", TypeName = "decimal(18,2)")]
    public decimal SchoolFeesPaid { get; set; }

    [Column("uniform_fees_paid", TypeName = "decimal(18,2)")]
    public decimal UniformFeesPaid { get; set; }

    [Column("bus_fees_paid", TypeName = "decimal(18,2)")]
    public decimal BusFeesPaid { get; set; }

    [Column("books_fees_paid", TypeName = "decimal(18,2)")]
    public decimal BooksFeesPaid { get; set; }

    [Column("payment_type")]
    [MaxLength(80)]
    public string? PaymentType { get; set; }

    public string? Notes { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }
}
