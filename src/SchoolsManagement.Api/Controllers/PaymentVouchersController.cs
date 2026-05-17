using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolsManagement.Api.Data;
using SchoolsManagement.Api.Models.Accounting;

namespace SchoolsManagement.Api.Controllers;

[ApiController]
[Route("api/payment-vouchers")]
[AllowAnonymous]
public class PaymentVouchersController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public PaymentVouchersController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PaymentVoucherRecord>>> List(CancellationToken ct)
    {
        var list = await _db.PaymentVouchers.AsNoTracking().OrderByDescending(x => x.Id).ToListAsync(ct);
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

        var entity = MapFromBody(body, id);
        _db.PaymentVouchers.Add(entity);
        await _db.SaveChangesAsync(ct);
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

        ApplyBody(entity, body);
        await _db.SaveChangesAsync(ct);
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

    private static PaymentVoucherRecord MapFromBody(PaymentVoucherRecord body, int id)
    {
        var now = DateTimeOffset.UtcNow;
        return new PaymentVoucherRecord
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
            CreatedBy = body.CreatedBy,
            BranchId = body.BranchId,
            CreatedAt = body.CreatedAt ?? now
        };
    }

    private static void ApplyBody(PaymentVoucherRecord entity, PaymentVoucherRecord body)
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
        entity.CreatedBy = body.CreatedBy;
        entity.BranchId = body.BranchId;
        entity.CreatedAt = body.CreatedAt ?? entity.CreatedAt;
    }
}
