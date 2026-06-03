using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace SchoolsManagement.Api.Models.Accounting;

[Table("receipt_vouchers")]
public class ReceiptVoucherRecord : IAccountingCreatedByAudit
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; }

    [Column("voucher_no")]
    [MaxLength(80)]
    public string VoucherNo { get; set; } = string.Empty;

    [Column("voucher_date")]
    public DateTimeOffset VoucherDate { get; set; }

    [Column("receipt_type")]
    [MaxLength(20)]
    public string ReceiptType { get; set; } = string.Empty;

    [Column("cash_box_account_id")]
    public int? CashBoxAccountId { get; set; }

    [Column("bank_account_id")]
    public int? BankAccountId { get; set; }

    [Column("transfer_no")]
    [MaxLength(120)]
    public string TransferNo { get; set; } = string.Empty;

    [Column("currency_id")]
    public int? CurrencyId { get; set; }

    [Column("amount", TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Column("account_id")]
    public int? AccountId { get; set; }

    [Column("analytic_account_id")]
    [MaxLength(200)]
    public string AnalyticAccountId { get; set; } = string.Empty;

    [Column("cost_center_id")]
    [MaxLength(200)]
    public string CostCenterId { get; set; } = string.Empty;

    [Column("journal_type_id")]
    public int? JournalTypeId { get; set; }

    [Column("notes")]
    public string Notes { get; set; } = string.Empty;

    [Column("handling")]
    [MaxLength(200)]
    public string Handling { get; set; } = string.Empty;

    [Column("created_by")]
    public int? CreatedBy { get; set; }

    [Column("created_by_user_id")]
    [MaxLength(450)]
    public string? CreatedByUserId { get; set; }

    [Column("created_by_name")]
    [MaxLength(300)]
    public string? CreatedByName { get; set; }

    [Column("branch_id")]
    public int? BranchId { get; set; }

    [Column("created_at")]
    public DateTimeOffset? CreatedAt { get; set; }
}
