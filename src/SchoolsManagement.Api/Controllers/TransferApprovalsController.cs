using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolsManagement.Api.Data;
using SchoolsManagement.Api.Models.School;

namespace SchoolsManagement.Api.Controllers;

[ApiController]
[Route("api/transfer-approvals")]
[AllowAnonymous]
public class TransferApprovalsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public TransferApprovalsController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> List(CancellationToken ct)
    {
        var rows = await _db.TransferApprovalRequests.AsNoTracking()
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);
        return Ok(rows.Select(ToDto));
    }

    [HttpPost("{id:long}/approve")]
    public async Task<IActionResult> Approve(long id, [FromBody] ApproveTransferRequest body, CancellationToken ct)
    {
        var row = await _db.TransferApprovalRequests.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (row is null)
        {
            return NotFound();
        }

        row.Status = "approved";
        row.BankId = body.BankId;
        row.Notes = body.Notes ?? row.Notes;
        row.ApprovedBy = body.ApprovedBy ?? "admin";
        row.ApprovedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static object ToDto(TransferApprovalRequestRecord r) => new
    {
        id = r.Id,
        parent_name = r.ParentName,
        student_id = r.StudentId,
        student_name = r.StudentName,
        amount = r.Amount,
        payment_method = r.PaymentMethod,
        transfer_no = r.TransferNo,
        bank_id = r.BankId,
        notes = r.Notes,
        status = r.Status,
        currency_id = r.CurrencyId,
        created_at = r.CreatedAt,
        approved_at = r.ApprovedAt,
        approved_by = r.ApprovedBy
    };
}

public class ApproveTransferRequest
{
    public int? BankId { get; set; }
    public string? Notes { get; set; }
    public string? ApprovedBy { get; set; }
}
