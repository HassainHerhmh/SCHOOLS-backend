using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolsManagement.Api.Data;
using SchoolsManagement.Api.Models.Accounting;
using SchoolsManagement.Api.Models.School;
using SchoolsManagement.Api.Services;

namespace SchoolsManagement.Api.Controllers;

[ApiController]
[Route("api/journal-posting")]
[AllowAnonymous]
public class JournalPostingController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public JournalPostingController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<object>> List(
        [FromQuery] bool pendingOnly = false,
        [FromQuery] bool postedOnly = false,
        CancellationToken ct = default)
    {
        await SalaryJournalPostingService.TryAutoPostAtMonthEndAsync(_db, ct);

        var query = _db.VoucherJournalEntries.AsNoTracking()
            .Where(x => x.Reference != null && x.Reference.StartsWith(SalaryJournalPostingService.ReferencePrefix));

        if (pendingOnly)
        {
            query = query.Where(x => x.PostedAt == null);
        }
        else if (postedOnly)
        {
            query = query.Where(x => x.PostedAt != null);
        }

        var rows = await query
            .OrderByDescending(x => x.EntryNumber)
            .ToListAsync(ct);

        var accountIds = rows
            .SelectMany(x => new[] { x.FromAccountId, x.ToAccountId })
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        var accountLabels = await LoadAccountLabelsAsync(accountIds, ct);

        var items = rows.Select(x => MapListItem(x, accountLabels)).ToList();
        return Ok(new
        {
            pending_count = rows.Count(x => x.PostedAt is null),
            items
        });
    }

    [HttpPost("post-all")]
    public async Task<ActionResult<object>> PostAllPending(CancellationToken ct)
    {
        var pending = await _db.VoucherJournalEntries
            .Where(x => x.PostedAt == null
                        && x.Reference != null
                        && x.Reference.StartsWith(SalaryJournalPostingService.ReferencePrefix))
            .ToListAsync(ct);

        if (pending.Count == 0)
        {
            return Ok(new { posted_count = 0, message = "لا توجد قيود بانتظار الترحيل." });
        }

        var now = DateTimeOffset.UtcNow;
        foreach (var entry in pending)
        {
            entry.PostedAt = now;
        }

        await _db.SaveChangesAsync(ct);
        return Ok(new { posted_count = pending.Count, posted_at = now });
    }

    [HttpPost("generate-salary-accruals")]
    public async Task<ActionResult<object>> GenerateSalaryAccruals(
        [FromBody] GenerateSalaryAccrualsRequest? body,
        CancellationToken ct)
    {
        var today = DateTime.UtcNow;
        var year = body?.Year is > 0 ? body.Year : today.Year;
        var month = body?.Month is >= 1 and <= 12 ? body.Month : today.Month;

        var transitSettings = await _db.TransitAccountsSettings.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == 1, ct);
        var transitAccountId = transitSettings?.CouponDiscountAccount;
        if (!transitAccountId.HasValue || transitAccountId.Value <= 0)
        {
            return BadRequest(new { message = "حدّد حساب وسيط مرتبات وأجور من صفحة الحسابات الوسيطة أولاً." });
        }

        var currencyId = await ResolveDefaultOperationalCurrencyIdAsync(ct);
        var employees = await _db.EmployeeRecords.AsNoTracking()
            .Where(e => e.Status == "active" && e.ChartAccountId.HasValue && e.ChartAccountId > 0)
            .ToListAsync(ct);

        if (employees.Count == 0)
        {
            return BadRequest(new { message = "لا يوجد موظفون نشطون مرتبطون بحساب محاسبي." });
        }

        var monthlyByEmployee = await _db.EmployeeMonthlyAccounts.AsNoTracking()
            .Where(m => m.Year == year && m.Month == month)
            .ToDictionaryAsync(m => m.EmployeeId, ct);

        var entryDate = SalaryJournalPostingService.LastDayOfMonthEntryDate(year, month);

        var created = 0;
        var updated = 0;
        var skipped = 0;
        var errors = new List<string>();

        foreach (var emp in employees)
        {
            var reference = BuildSalaryReference(year, month, emp.Id);
            var amount = ResolveGrossSalaryAmount(emp, monthlyByEmployee);
            if (amount <= 0)
            {
                skipped++;
                continue;
            }

            var existing = await _db.VoucherJournalEntries
                .FirstOrDefaultAsync(x => x.Reference == reference, ct);
            if (existing is not null)
            {
                if (existing.PostedAt is not null)
                {
                    skipped++;
                    continue;
                }

                existing.Amount = amount;
                existing.EntryDate = entryDate;
                existing.Description = BuildSalaryDescription(month, year, emp.Name);
                existing.FromAccountId = transitAccountId.Value;
                existing.ToAccountId = emp.ChartAccountId!.Value;
                existing.CurrencyId = currencyId;
                updated++;
                continue;
            }

            var entryNumber = await NextJournalEntryNumberAsync(ct);
            var now = DateTimeOffset.UtcNow;
            _db.VoucherJournalEntries.Add(new VoucherJournalEntryRecord
            {
                Id = Guid.NewGuid(),
                EntryNumber = entryNumber,
                EntryDate = entryDate,
                Description = BuildSalaryDescription(month, year, emp.Name),
                FromAccountId = transitAccountId.Value,
                ToAccountId = emp.ChartAccountId!.Value,
                CurrencyId = currencyId,
                Amount = amount,
                Reference = reference,
                CreatedAt = now,
                PostedAt = null
            });
            created++;
        }

        if (created > 0 || updated > 0)
        {
            await _db.SaveChangesAsync(ct);
        }

        return Ok(new
        {
            year,
            month,
            created_count = created,
            updated_count = updated,
            skipped_count = skipped,
            errors
        });
    }

    private static decimal ResolveGrossSalaryAmount(
        EmployeeRecord emp,
        IReadOnlyDictionary<Guid, EmployeeMonthlyAccountRecord> monthlyByEmployee)
    {
        if (monthlyByEmployee.TryGetValue(emp.Id, out var monthly) && monthly.GrossSalary > 0)
        {
            return monthly.GrossSalary;
        }

        return emp.BaseSalary + emp.Allowances;
    }

    private static string BuildSalaryDescription(int month, int year, string employeeName) =>
        $"استحقاق راتب إجمالي شهر {month}/{year} — {employeeName}";

    private static string BuildSalaryReference(int year, int month, Guid employeeId) =>
        $"{SalaryJournalPostingService.ReferencePrefix}{year:D4}{month:D2}-{employeeId:N}";

    private async Task<int> NextJournalEntryNumberAsync(CancellationToken ct)
    {
        var max = await _db.VoucherJournalEntries.Select(x => (int?)x.EntryNumber).MaxAsync(ct) ?? 0;
        return Math.Max(max, 1000) + 1;
    }

    private async Task<int?> ResolveDefaultOperationalCurrencyIdAsync(CancellationToken ct)
    {
        var list = await _db.Currencies.AsNoTracking().OrderBy(c => c.Id).ToListAsync(ct);
        if (list.Count == 0)
        {
            return null;
        }

        var local = list.FirstOrDefault(c => c.IsLocal);
        if (local is not null)
        {
            return local.Id;
        }

        static bool IsYemeniRial(CurrencyRecord c)
        {
            var code = (c.Code ?? "").Trim().ToUpperInvariant();
            if (code is "YER" or "YRL" or "RY")
            {
                return true;
            }

            var name = c.NameAr ?? "";
            return name.Contains("ريال يمني", StringComparison.Ordinal)
                   || name.Contains("الريال اليمني", StringComparison.Ordinal);
        }

        var yer = list.FirstOrDefault(IsYemeniRial);
        return yer?.Id ?? list[0].Id;
    }

    private async Task<Dictionary<int, string>> LoadAccountLabelsAsync(
        IReadOnlyCollection<int> accountIds,
        CancellationToken ct)
    {
        if (accountIds.Count == 0)
        {
            return new Dictionary<int, string>();
        }

        var rows = await ChartAccountSqlLookup.GetByIdsAsync(_db, accountIds.ToList(), ct);
        return rows.Values.ToDictionary(
            a => a.Id,
            a =>
            {
                var code = (a.Code ?? "").Trim();
                var name = (a.NameAr ?? "").Trim();
                return string.IsNullOrEmpty(code) ? name : $"{code} - {name}";
            });
    }

    private static object MapListItem(
        VoucherJournalEntryRecord x,
        IReadOnlyDictionary<int, string> accountLabels)
    {
        string? Label(int? id) =>
            id.HasValue && accountLabels.TryGetValue(id.Value, out var label) ? label : null;

        return new
        {
            id = x.Id,
            entry_number = x.EntryNumber,
            entry_date = x.EntryDate,
            description = x.Description,
            from_account_id = x.FromAccountId,
            from_account_label = Label(x.FromAccountId),
            to_account_id = x.ToAccountId,
            to_account_label = Label(x.ToAccountId),
            amount = x.Amount,
            reference = x.Reference,
            posted_at = x.PostedAt,
            is_pending = x.PostedAt is null,
            is_salary_accrual = SalaryJournalPostingService.IsSalaryAccrualReference(x.Reference)
        };
    }

    public sealed class GenerateSalaryAccrualsRequest
    {
        public int Year { get; set; }
        public int Month { get; set; }
    }
}
