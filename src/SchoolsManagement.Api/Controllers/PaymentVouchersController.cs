using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolsManagement.Api.Data;
using SchoolsManagement.Api.Models.Accounting;
using SchoolsManagement.Api.Services;

namespace SchoolsManagement.Api.Controllers;

[ApiController]
[Route("api/payment-vouchers")]
[Authorize]
public class PaymentVouchersController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly AccountingCurrentUserService _currentUser;
    private readonly VoucherUserNameEnricher _userNameEnricher;

    public PaymentVouchersController(
        ApplicationDbContext db,
        AccountingCurrentUserService currentUser,
        VoucherUserNameEnricher userNameEnricher)
    {
        _db = db;
        _currentUser = currentUser;
        _userNameEnricher = userNameEnricher;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PaymentVoucherRecord>>> List(CancellationToken ct)
    {
        var list = await _db.PaymentVouchers.AsNoTracking().OrderByDescending(x => x.Id).ToListAsync(ct);
        await AccountingVoucherAuditHelper.EnrichDisplayNamesAsync(list, _userNameEnricher, ct);
        return Ok(list);
    }

    [HttpGet("next-id")]
    public async Task<ActionResult<object>> NextId(CancellationToken ct)
    {
        var max = await _db.PaymentVouchers.Select(x => (int?)x.Id).MaxAsync(ct) ?? 0;
        return Ok(new { next_id = max + 1 });
    }

    [HttpPost]
    public async Task<ActionResult<PaymentVoucherRecord>> Create([FromBody] PaymentVoucherRecord body, CancellationToken ct)
    {
        var id = body.Id <= 0 ? await NextIdInt(ct) : body.Id;
        if (await _db.PaymentVouchers.AnyAsync(x => x.Id == id, ct))
        {
            return Conflict(new { message = "معرّف السند مستخدم مسبقاً." });
        }

        var entity = await MapFromBodyAsync(body, id, ct);
        _db.PaymentVouchers.Add(entity);
        await _db.SaveChangesAsync(ct);
        await AccountingVoucherAuditHelper.EnrichDisplayNamesAsync([entity], _userNameEnricher, ct);
        return CreatedAtAction(nameof(List), entity);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<PaymentVoucherRecord>> Update(int id, [FromBody] PaymentVoucherRecord body, CancellationToken ct)
    {
        var entity = await _db.PaymentVouchers.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null)
        {
            return NotFound();
        }

        await ApplyBodyAsync(entity, body, ct);
        await _db.SaveChangesAsync(ct);
        await AccountingVoucherAuditHelper.EnrichDisplayNamesAsync([entity], _userNameEnricher, ct);
        return Ok(entity);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await _db.PaymentVouchers.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null)
        {
            return NotFound();
        }

        _db.PaymentVouchers.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    private async Task<int> NextIdInt(CancellationToken ct)
    {
        var max = await _db.PaymentVouchers.Select(x => (int?)x.Id).MaxAsync(ct) ?? 0;
        return max + 1;
    }

    private async Task<PaymentVoucherRecord> MapFromBodyAsync(PaymentVoucherRecord body, int id, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var entity = new PaymentVoucherRecord
        {
            Id = id,
            VoucherNo = body.VoucherNo?.Trim() ?? "",
            VoucherDate = body.VoucherDate == default ? now : body.VoucherDate,
            PaymentType = body.PaymentType ?? "",
            CashBoxAccountId = body.CashBoxAccountId,
            BankAccountId = body.BankAccountId,
            TransferNo = body.TransferNo ?? "",
            CurrencyId = body.CurrencyId,
            Amount = body.Amount,
            AccountId = body.AccountId,
            AnalyticAccountId = body.AnalyticAccountId ?? "",
            CostCenterId = body.CostCenterId ?? "",
            JournalTypeId = body.JournalTypeId,
            Notes = body.Notes ?? "",
            Handling = body.Handling ?? "",
            BranchId = body.BranchId,
            CreatedAt = body.CreatedAt ?? now
        };
        await AccountingVoucherAuditHelper.ApplyOnCreateAsync(entity, body, _currentUser, ct);
        return entity;
    }

    private async Task ApplyBodyAsync(PaymentVoucherRecord entity, PaymentVoucherRecord body, CancellationToken ct)
    {
        entity.VoucherNo = body.VoucherNo?.Trim() ?? entity.VoucherNo;
        entity.VoucherDate = body.VoucherDate == default ? entity.VoucherDate : body.VoucherDate;
        entity.PaymentType = body.PaymentType ?? entity.PaymentType;
        entity.CashBoxAccountId = body.CashBoxAccountId;
        entity.BankAccountId = body.BankAccountId;
        entity.TransferNo = body.TransferNo ?? "";
        entity.CurrencyId = body.CurrencyId;
        entity.Amount = body.Amount;
        entity.AccountId = body.AccountId;
        entity.AnalyticAccountId = body.AnalyticAccountId ?? "";
        entity.CostCenterId = body.CostCenterId ?? "";
        entity.JournalTypeId = body.JournalTypeId;
        entity.Notes = body.Notes ?? "";
        entity.Handling = body.Handling ?? "";
        entity.BranchId = body.BranchId;
        entity.CreatedAt = body.CreatedAt ?? entity.CreatedAt;
        await AccountingVoucherAuditHelper.ApplyOnUpdateAsync(entity, body, _currentUser, ct);
    }
}
