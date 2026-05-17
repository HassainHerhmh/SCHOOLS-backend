using System.ComponentModel.DataAnnotations;

namespace SchoolsManagement.Api.Models.Accounting;

public class UpsertCurrencyRequest
{
    [Required]
    [MaxLength(500)]
    public string NameAr { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Symbol { get; set; } = string.Empty;

    public decimal ExchangeRate { get; set; }

    public decimal? MinRate { get; set; }

    public decimal? MaxRate { get; set; }

    public bool IsLocal { get; set; }

    /// <summary>* أو / فقط</summary>
    [RegularExpression(@"^(\*|/)$")]
    [MaxLength(1)]
    public string ConvertMode { get; set; } = "*";
}
