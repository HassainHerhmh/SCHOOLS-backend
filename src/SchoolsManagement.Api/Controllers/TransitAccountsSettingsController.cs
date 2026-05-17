using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolsManagement.Api.Data;
using SchoolsManagement.Api.Models.Accounting;

namespace SchoolsManagement.Api.Controllers;

[ApiController]
[Route("api/transit-accounts-settings")]
[AllowAnonymous]
public class TransitAccountsSettingsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public TransitAccountsSettingsController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<TransitAccountsSettingsRecord>> Get(CancellationToken cancellationToken)
    {
        var record = await _db.TransitAccountsSettings.FirstOrDefaultAsync(x => x.Id == 1, cancellationToken);
        if (record is null)
        {
            return Ok(new TransitAccountsSettingsRecord { Id = 1 });
        }
        return Ok(record);
    }

    [HttpPost]
    public async Task<ActionResult<TransitAccountsSettingsRecord>> Upsert(
        [FromBody] UpsertTransitAccountsSettingsRequest body,
        CancellationToken cancellationToken)
    {
        var record = await _db.TransitAccountsSettings.FirstOrDefaultAsync(x => x.Id == 1, cancellationToken);

        if (record is null)
        {
            record = new TransitAccountsSettingsRecord { Id = 1 };
            _db.TransitAccountsSettings.Add(record);
        }

        record.StudentInstallmentsTransitAccount = body.StudentInstallmentsTransitAccount;
        record.CourierCommissionAccount = body.CourierCommissionAccount;
        record.CouponDiscountAccount = body.CouponDiscountAccount;
        record.TransferGuaranteeAccount = body.TransferGuaranteeAccount;
        record.CurrencyExchangeAccount = body.CurrencyExchangeAccount;
        record.CustomerGuaranteeAccount = body.CustomerGuaranteeAccount;
        record.CustomerCreditAccount = body.CustomerCreditAccount;
        record.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return Ok(record);
    }
}
