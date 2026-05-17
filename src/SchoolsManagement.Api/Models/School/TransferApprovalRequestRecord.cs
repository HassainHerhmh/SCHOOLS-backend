using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolsManagement.Api.Models.School;

[Table("transfer_approval_requests")]
public class TransferApprovalRequestRecord
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [Column("parent_name")]
    [MaxLength(300)]
    public string ParentName { get; set; } = string.Empty;

    [Column("student_id")]
    public Guid? StudentId { get; set; }

    [Column("student_name")]
    [MaxLength(300)]
    public string? StudentName { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Column("payment_method")]
    [MaxLength(80)]
    public string PaymentMethod { get; set; } = string.Empty;

    [Column("transfer_no")]
    [MaxLength(120)]
    public string TransferNo { get; set; } = string.Empty;

    [Column("bank_id")]
    public int? BankId { get; set; }

    public string? Notes { get; set; }

    [MaxLength(40)]
    public string Status { get; set; } = "pending";

    [Column("currency_id")]
    public int? CurrencyId { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [Column("approved_at")]
    public DateTimeOffset? ApprovedAt { get; set; }

    [Column("approved_by")]
    [MaxLength(200)]
    public string? ApprovedBy { get; set; }
}
