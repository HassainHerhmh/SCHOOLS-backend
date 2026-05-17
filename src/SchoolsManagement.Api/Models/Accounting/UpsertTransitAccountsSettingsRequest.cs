namespace SchoolsManagement.Api.Models.Accounting;

public class UpsertTransitAccountsSettingsRequest
{
    public int? StudentInstallmentsTransitAccount { get; set; }

    /// <summary>حساب وسيط خصومات الموظفين (JSON/API التاريخي: courier_commission_account).</summary>
    public int? CourierCommissionAccount { get; set; }
    public int? CouponDiscountAccount { get; set; }
    /// <summary>حساب وسيط مكافآت الموظفين (JSON/API التاريخي: transfer_guarantee_account).</summary>
    public int? TransferGuaranteeAccount { get; set; }
    public int? CurrencyExchangeAccount { get; set; }
    public int? CustomerGuaranteeAccount { get; set; }
    public int? CustomerCreditAccount { get; set; }
}
