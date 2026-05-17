using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolsManagement.Api.Models.Accounting;

/// <summary>قيد يومية مرتبط بسندات القبض/الصرف (جدول journal_entries في Supabase سابقاً).</summary>
[Table("journal_entries")]
public class VoucherJournalEntryRecord
{
    [Key]
    public Guid Id { get; set; }

    [Column("entry_number")]
    public int EntryNumber { get; set; }

    [Column("entry_date")]
    public DateTimeOffset EntryDate { get; set; }

    [Column("description")]
    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Column("from_account_id")]
    public int? FromAccountId { get; set; }

    [Column("to_account_id")]
    public int? ToAccountId { get; set; }

    [Column("currency_id")]
    public int? CurrencyId { get; set; }

    [Column("amount", TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Column("reference")]
    [MaxLength(200)]
    public string Reference { get; set; } = string.Empty;

    [Column("created_by")]
    public int? CreatedBy { get; set; }

    [Column("branch_id")]
    public int? BranchId { get; set; }

    [Column("created_at")]
    public DateTimeOffset? CreatedAt { get; set; }

    /// <summary>تاريخ ترحيل القيد؛ إن كان null فالقيد بانتظار الترحيل.</summary>
    [Column("posted_at")]
    public DateTimeOffset? PostedAt { get; set; }
}
