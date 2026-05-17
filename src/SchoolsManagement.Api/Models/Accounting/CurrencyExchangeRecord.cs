using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolsManagement.Api.Models.Accounting;

[Table("currency_exchanges")]
public class CurrencyExchangeRecord
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; }

    [Column("reference")]
    [MaxLength(200)]
    public string Reference { get; set; } = string.Empty;

    [Column("exchange_date")]
    public DateTimeOffset ExchangeDate { get; set; }

    [Column("exchange_type")]
    [MaxLength(20)]
    public string ExchangeType { get; set; } = string.Empty;

    [Column("from_currency_id")]
    public int? FromCurrencyId { get; set; }

    [Column("from_amount", TypeName = "decimal(18,2)")]
    public decimal FromAmount { get; set; }

    [Column("from_rate", TypeName = "decimal(18,6)")]
    public decimal FromRate { get; set; }

    [Column("from_account_id")]
    public int? FromAccountId { get; set; }

    [Column("to_currency_id")]
    public int? ToCurrencyId { get; set; }

    [Column("to_amount", TypeName = "decimal(18,2)")]
    public decimal ToAmount { get; set; }

    [Column("to_rate", TypeName = "decimal(18,6)")]
    public decimal ToRate { get; set; }

    [Column("to_account_id")]
    public int? ToAccountId { get; set; }

    [Column("customer_name")]
    [MaxLength(300)]
    public string CustomerName { get; set; } = string.Empty;

    [Column("notes")]
    public string Notes { get; set; } = string.Empty;

    [Column("created_by")]
    public int? CreatedBy { get; set; }

    [Column("branch_id")]
    public int? BranchId { get; set; }

    [Column("created_at")]
    public DateTimeOffset? CreatedAt { get; set; }
}
