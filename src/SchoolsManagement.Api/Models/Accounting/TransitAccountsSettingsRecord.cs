using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolsManagement.Api.Models.Accounting;

[Table("transit_accounts_settings")]
public class TransitAccountsSettingsRecord
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; } = 1;

    [Column("student_installments_transit_account")]
    public int? StudentInstallmentsTransitAccount { get; set; }

    /// <summary>حساب وسيط خصومات الموظفين (العمود التاريخي في DB: courier_commission_account).</summary>
    [Column("courier_commission_account")]
    public int? CourierCommissionAccount { get; set; }

    /// <summary>حساب وسيط مرتبات وأجور (العمود التاريخي في DB: coupon_discount_account).</summary>
    [Column("coupon_discount_account")]
    public int? CouponDiscountAccount { get; set; }

    /// <summary>حساب وسيط مكافآت الموظفين (العمود التاريخي في DB: transfer_guarantee_account).</summary>
    [Column("transfer_guarantee_account")]
    public int? TransferGuaranteeAccount { get; set; }

    [Column("currency_exchange_account")]
    public int? CurrencyExchangeAccount { get; set; }

    [Column("customer_guarantee_account")]
    public int? CustomerGuaranteeAccount { get; set; }

    [Column("customer_credit_account")]
    public int? CustomerCreditAccount { get; set; }

    [Column("updated_at")]
    public DateTimeOffset? UpdatedAt { get; set; }
}
