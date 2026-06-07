using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolsManagement.Api.Models.School;

[Table("parents_student_installments")]
public class ParentsStudentInstallmentRecord
{
    [Column("student_id")]
    public Guid StudentId { get; set; }

    [Column("fee_kind")]
    [MaxLength(40)]
    public string FeeKind { get; set; } = string.Empty;

    [Column("slot_index")]
    public int SlotIndex { get; set; }

    [Column("label")]
    [MaxLength(200)]
    public string Label { get; set; } = string.Empty;

    [Column("due", TypeName = "decimal(18,2)")]
    public decimal Due { get; set; }

    [Column("paid", TypeName = "decimal(18,2)")]
    public decimal Paid { get; set; }

    [Column("remaining", TypeName = "decimal(18,2)")]
    public decimal Remaining { get; set; }

    [Column("is_fully_paid")]
    public bool IsFullyPaid { get; set; }

    [Column("synced_at")]
    public DateTimeOffset SyncedAt { get; set; }
}
