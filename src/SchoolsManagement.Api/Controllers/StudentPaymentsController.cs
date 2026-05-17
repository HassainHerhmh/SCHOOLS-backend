using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolsManagement.Api.Data;
using SchoolsManagement.Api.Models.School;

namespace SchoolsManagement.Api.Controllers;

[ApiController]
[Route("api/student-payments")]
[AllowAnonymous]
public class StudentPaymentsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public StudentPaymentsController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> List(
        [FromQuery] int? year,
        [FromQuery] Guid? studentId,
        CancellationToken ct)
    {
        var q = _db.StudentPayments.AsNoTracking();
        if (studentId.HasValue && studentId.Value != Guid.Empty)
        {
            q = q.Where(p => p.StudentId == studentId.Value);
        }

        if (year.HasValue)
        {
            var start = new DateOnly(year.Value, 1, 1);
            var end = new DateOnly(year.Value, 12, 31);
            q = q.Where(p => p.PaymentDate >= start && p.PaymentDate <= end);
        }

        var rows = await q.OrderByDescending(p => p.PaymentDate).ThenByDescending(p => p.CreatedAt).ToListAsync(ct);
        return Ok(rows.Select(ToDto));
    }

    [HttpGet("stats")]
    public async Task<ActionResult<object>> Stats([FromQuery] int year, CancellationToken ct)
    {
        var start = new DateOnly(year, 1, 1);
        var end = new DateOnly(year, 12, 31);
        var payments = await _db.StudentPayments.AsNoTracking()
            .Where(p => p.PaymentDate >= start && p.PaymentDate <= end)
            .ToListAsync(ct);
        var totalPaid = payments.Sum(p => p.Amount);
        return Ok(new { totalPaid, totalReceipts = payments.Count });
    }

    [HttpGet("next-receipt-number")]
    public async Task<ActionResult<object>> NextReceiptNumber(CancellationToken ct)
    {
        var last = await _db.StudentPayments.AsNoTracking()
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => p.ReceiptNumber)
            .FirstOrDefaultAsync(ct);
        var next = 1;
        if (!string.IsNullOrEmpty(last))
        {
            var yearSuffix = DateTime.UtcNow.ToString("yy");
            var numericPart = last.Replace("RCP-", "").Replace("/" + yearSuffix, "");
            if (int.TryParse(numericPart, out var n))
            {
                next = n + 1;
            }
        }

        return Ok(new { receiptNumber = FormatReceiptNumber(next) });
    }

    [HttpPost]
    public async Task<ActionResult<object>> Create([FromBody] UpsertStudentPaymentRequest body, CancellationToken ct)
    {
        if (body.StudentId == Guid.Empty)
        {
            return BadRequest(new { message = "معرّف الطالب مطلوب." });
        }

        var receipt = body.ReceiptNumber;
        if (string.IsNullOrWhiteSpace(receipt))
        {
            var last = await _db.StudentPayments.AsNoTracking()
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => p.ReceiptNumber)
                .FirstOrDefaultAsync(ct);
            var nextNum = 1;
            if (!string.IsNullOrEmpty(last))
            {
                var yearSuffix = DateTime.UtcNow.ToString("yy");
                var numericPart = last.Replace("RCP-", "").Replace("/" + yearSuffix, "");
                if (int.TryParse(numericPart, out var n))
                {
                    nextNum = n + 1;
                }
            }

            receipt = FormatReceiptNumber(nextNum);
        }

        var entity = new StudentPaymentRecord
        {
            Id = Guid.NewGuid(),
            StudentId = body.StudentId,
            StudentName = body.StudentName,
            Amount = body.Amount,
            PaymentDate = body.PaymentDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
            ReceiptNumber = receipt.Trim(),
            SchoolFeesPaid = body.SchoolFeesPaid,
            UniformFeesPaid = body.UniformFeesPaid,
            BusFeesPaid = body.BusFeesPaid,
            BooksFeesPaid = body.BooksFeesPaid,
            PaymentType = body.PaymentType,
            Notes = body.Notes,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.StudentPayments.Add(entity);
        await _db.SaveChangesAsync(ct);
        return Created($"/api/student-payments/{entity.Id}", ToDto(entity));
    }

    private static string FormatReceiptNumber(int number)
    {
        var year = DateTime.UtcNow.ToString("yy");
        return $"RCP-{number.ToString().PadLeft(5, '0')}/{year}";
    }

    private static object ToDto(StudentPaymentRecord p) => new
    {
        id = p.Id,
        student_id = p.StudentId,
        student_name = p.StudentName,
        amount = p.Amount,
        payment_date = p.PaymentDate.ToString("yyyy-MM-dd"),
        receipt_number = p.ReceiptNumber,
        school_fees_paid = p.SchoolFeesPaid,
        uniform_fees_paid = p.UniformFeesPaid,
        bus_fees_paid = p.BusFeesPaid,
        books_fees_paid = p.BooksFeesPaid,
        payment_type = p.PaymentType,
        notes = p.Notes,
        created_at = p.CreatedAt
    };
}

public class UpsertStudentPaymentRequest
{
    public Guid StudentId { get; set; }
    public string? StudentName { get; set; }
    public decimal Amount { get; set; }
    public DateOnly? PaymentDate { get; set; }
    public string? ReceiptNumber { get; set; }
    public decimal SchoolFeesPaid { get; set; }
    public decimal UniformFeesPaid { get; set; }
    public decimal BusFeesPaid { get; set; }
    public decimal BooksFeesPaid { get; set; }
    public string? PaymentType { get; set; }
    public string? Notes { get; set; }
}
