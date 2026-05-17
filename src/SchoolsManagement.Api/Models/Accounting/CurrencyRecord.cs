using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolsManagement.Api.Models.Accounting;

[Table("currencies")]
public class CurrencyRecord
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Column("name_ar")]
    [MaxLength(500)]
    public string NameAr { get; set; } = string.Empty;

    [Column("code")]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [Column("symbol")]
    [MaxLength(50)]
    public string Symbol { get; set; } = string.Empty;

    [Column("exchange_rate", TypeName = "decimal(18,6)")]
    public decimal ExchangeRate { get; set; }

    [Column("min_rate", TypeName = "decimal(18,6)")]
    public decimal? MinRate { get; set; }

    [Column("max_rate", TypeName = "decimal(18,6)")]
    public decimal? MaxRate { get; set; }

    [Column("is_local")]
    public bool IsLocal { get; set; }

    /// <summary>ضرب أو قسمة؛ قيمة واحدة: * أو /</summary>
    [Column("convert_mode")]
    [MaxLength(1)]
    public string ConvertMode { get; set; } = "*";
}
