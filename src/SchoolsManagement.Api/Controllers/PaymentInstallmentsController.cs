using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolsManagement.Api.Data;
using SchoolsManagement.Api.Models.School;
using SchoolsManagement.Api.Services;
using System.Text.Json;

namespace SchoolsManagement.Api.Controllers;

[ApiController]
[Route("api/payment-installments")]
[AllowAnonymous]
public class PaymentInstallmentsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private static readonly string[] DefaultSchoolYearMonths =
    [
        "سبتمبر", "أكتوبر", "نوفمبر", "ديسمبر", "يناير", "فبراير",
        "مارس", "أبريل", "مايو", "يونيو", "يوليو", "أغسطس"
    ];

    public PaymentInstallmentsController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet("settings")]
    public async Task<ActionResult<object>> GetSettings(CancellationToken ct)
    {
        var row = await EnsureSettingsRowAsync(ct);
        return Ok(new
        {
            tuition_installments_count = row.TuitionInstallmentsCount,
            bus_installments_count = row.BusInstallmentsCount,
            tuition_month_labels = ParseTuitionMonthLabels(row, row.TuitionInstallmentsCount)
        });
    }

    [HttpPut("settings")]
    public async Task<ActionResult<object>> SaveSettings([FromBody] SettingsRequest? body, CancellationToken ct)
    {
        body ??= new SettingsRequest();
        var tuition = body.TuitionInstallmentsCount > 0
            ? body.TuitionInstallmentsCount
            : body.TuitionInstallmentsCountAlt;
        var bus = body.BusInstallmentsCount > 0
            ? body.BusInstallmentsCount
            : body.BusInstallmentsCountAlt;

        if (tuition < 1 || tuition > 12 || bus < 1 || bus > 12)
        {
            return BadRequest(new { message = "عدد الأقساط يجب أن يكون بين 1 و 12." });
        }

        var row = await EnsureSettingsRowAsync(ct);
        row.TuitionInstallmentsCount = tuition;
        row.BusInstallmentsCount = bus;
        if (body.TuitionMonthLabels is { Count: > 0 })
        {
            row.TuitionMonthLabelsJson = JsonSerializer.Serialize(
                NormalizeTuitionMonthLabels(tuition, body.TuitionMonthLabels));
        }
        else if (body.TuitionMonthLabelsJson is not null)
        {
            row.TuitionMonthLabelsJson = body.TuitionMonthLabelsJson;
        }
        else if (string.IsNullOrWhiteSpace(row.TuitionMonthLabelsJson))
        {
            row.TuitionMonthLabelsJson = JsonSerializer.Serialize(DefaultTuitionMonthLabels(tuition));
        }
        row.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);

        return Ok(new
        {
            tuition_installments_count = row.TuitionInstallmentsCount,
            bus_installments_count = row.BusInstallmentsCount,
            tuition_month_labels = ParseTuitionMonthLabels(row, row.TuitionInstallmentsCount)
        });
    }

    [HttpGet("students/{id:guid}/status")]
    public async Task<ActionResult<object>> GetStudentStatus(
        Guid id,
        [FromQuery] string? fee_kind,
        [FromQuery] string? feeKind,
        [FromQuery] decimal? preview_amount,
        [FromQuery] decimal? previewAmount,
        CancellationToken ct)
    {
        var kind = (fee_kind ?? feeKind ?? string.Empty).Trim();
        if (kind is not ("school_fees" or "bus_fees"))
        {
            return BadRequest(new { message = "fee_kind يجب أن يكون school_fees أو bus_fees." });
        }

        var student = await _db.StudentRecords.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, ct);
        if (student is null)
        {
            return NotFound();
        }

        var settings = await EnsureSettingsRowAsync(ct);
        var preview = preview_amount ?? previewAmount;
        var status = BuildStatus(student, settings, kind, preview);
        return Ok(status);
    }

    private async Task<PaymentInstallmentSettingsRecord> EnsureSettingsRowAsync(CancellationToken ct)
    {
        var row = await _db.PaymentInstallmentSettings.FirstOrDefaultAsync(s => s.Id == 1, ct);
        if (row is not null)
        {
            return row;
        }

        row = new PaymentInstallmentSettingsRecord
        {
            Id = 1,
            TuitionInstallmentsCount = 6,
            BusInstallmentsCount = 2,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        _db.PaymentInstallmentSettings.Add(row);
        await _db.SaveChangesAsync(ct);
        return row;
    }

    private static object BuildStatus(
        StudentRecord student,
        PaymentInstallmentSettingsRecord settings,
        string feeKind,
        decimal? previewAmount)
    {
        var totalFees = feeKind == "bus_fees" ? student.BusFees : student.SchoolFees;
        var totalPaid = feeKind == "bus_fees"
            ? student.PaidBusFees ?? 0
            : student.PaidSchoolFees ?? 0;
        var count = feeKind == "bus_fees"
            ? settings.BusInstallmentsCount
            : settings.TuitionInstallmentsCount;
        var monthLabels = feeKind == "school_fees"
            ? ParseTuitionMonthLabels(settings, count)
            : null;

        var slots = PaymentInstallmentCalculator.BuildSlots(totalFees, count, totalPaid, feeKind, monthLabels);
        var remainingCount = PaymentInstallmentCalculator.RemainingInstallmentsCount(slots);

        object? preview = null;
        if (previewAmount.HasValue && previewAmount.Value > 0)
        {
            var allocation = PaymentInstallmentCalculator.SimulateReceiptAllocation(
                totalFees,
                count,
                totalPaid,
                previewAmount.Value,
                feeKind,
                monthLabels);
            preview = new
            {
                applied_amount = Math.Min(previewAmount.Value, Math.Max(0, totalFees - totalPaid)),
                allocation = allocation.Select(a => new { label = a.Label, amount = a.Amount }),
                allocation_note = PaymentInstallmentCalculator.FormatAllocationNote(allocation)
            };
        }

        return new
        {
            fee_kind = feeKind,
            total_fees = totalFees,
            total_paid = totalPaid,
            total_remaining = Math.Max(0, totalFees - totalPaid),
            installments_count = PaymentInstallmentCalculator.NormalizeCount(count),
            remaining_installments_count = remainingCount,
            slots = slots.Select(s => new
            {
                index = s.Index,
                label = s.Label,
                due = s.Due,
                paid = s.Paid,
                remaining = s.Remaining,
                is_fully_paid = s.IsFullyPaid
            }),
            preview
        };
    }

    public sealed class SettingsRequest
    {
        public int TuitionInstallmentsCount { get; set; }
        public int TuitionInstallmentsCountAlt { get; set; }
        public int BusInstallmentsCount { get; set; }
        public int BusInstallmentsCountAlt { get; set; }
        public List<string>? TuitionMonthLabels { get; set; }
        public string? TuitionMonthLabelsJson { get; set; }
    }

    private static List<string> DefaultTuitionMonthLabels(int count)
    {
        count = PaymentInstallmentCalculator.NormalizeCount(count);
        return DefaultSchoolYearMonths.Take(count).ToList();
    }

    private static List<string> NormalizeTuitionMonthLabels(int count, IEnumerable<string>? labels)
    {
        count = PaymentInstallmentCalculator.NormalizeCount(count);
        var defaults = DefaultTuitionMonthLabels(count);
        var source = labels?.Select(x => (x ?? string.Empty).Trim()).ToList() ?? [];
        return Enumerable.Range(0, count)
            .Select(i => i < source.Count && !string.IsNullOrWhiteSpace(source[i]) ? source[i] : defaults[i])
            .ToList();
    }

    private static List<string> ParseTuitionMonthLabels(PaymentInstallmentSettingsRecord settings, int count)
    {
        count = PaymentInstallmentCalculator.NormalizeCount(count);
        if (!string.IsNullOrWhiteSpace(settings.TuitionMonthLabelsJson))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<List<string>>(settings.TuitionMonthLabelsJson);
                if (parsed is { Count: > 0 })
                {
                    return NormalizeTuitionMonthLabels(count, parsed);
                }
            }
            catch
            {
                /* fallback below */
            }
        }

        return DefaultTuitionMonthLabels(count);
    }
}
