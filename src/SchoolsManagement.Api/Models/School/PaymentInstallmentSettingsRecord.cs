using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolsManagement.Api.Models.School;

[Table("payment_installment_settings")]
public class PaymentInstallmentSettingsRecord
{
    [Key]
    [Column("id")]
    public int Id { get; set; } = 1;

    [Column("tuition_installments_count")]
    public int TuitionInstallmentsCount { get; set; } = 6;

    [Column("bus_installments_count")]
    public int BusInstallmentsCount { get; set; } = 2;

    /// <summary>JSON array — أسماء الأشهر لكل قسط دراسة، مثل ["سبتمبر","أكتوبر",...]</summary>
    [Column("tuition_month_labels")]
    public string? TuitionMonthLabelsJson { get; set; }

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }
}
