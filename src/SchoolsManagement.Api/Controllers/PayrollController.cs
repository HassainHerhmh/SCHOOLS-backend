using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolsManagement.Api.Data;
using SchoolsManagement.Api.Models.Accounting;
using SchoolsManagement.Api.Models.School;
using SchoolsManagement.Api.Services;

namespace SchoolsManagement.Api.Controllers;

[ApiController]
[Route("api/payroll")]
[AllowAnonymous]
public class PayrollController : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private readonly ApplicationDbContext _db;

    public PayrollController(ApplicationDbContext db)
    {
        _db = db;
    }

    private static JsonNode ParseArr(string json) =>
        JsonSerializer.Deserialize<JsonNode>(string.IsNullOrWhiteSpace(json) ? "[]" : json, JsonOpts)
        ?? new JsonArray();

    private static decimal SumAmounts(JsonArray arr, string prop = "amount")
    {
        decimal sum = 0;
        foreach (var node in arr)
        {
            if (node is null) continue;
            var amtNode = node[prop];
            if (amtNode is null) continue;
            var s = amtNode.ToString();
            if (string.IsNullOrEmpty(s))
            {
                continue;
            }

            if (decimal.TryParse(s, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var dec))
            {
                sum += dec;
            }
        }

        return sum;
    }

    private static decimal RecalcNet(decimal gross, decimal totalDed, decimal totalBonus) =>
        gross - totalDed + totalBonus;

    private async Task<int> NextJournalEntryNumberAsync(CancellationToken ct)
    {
        var max = await _db.VoucherJournalEntries.Select(x => (int?)x.EntryNumber).MaxAsync(ct) ?? 0;
        return Math.Max(max, 1000) + 1;
    }

    private static DateTimeOffset ParseDeductionEntryDate(JsonObject node)
    {
        var fallback = DateTimeOffset.UtcNow;
        var dateNode = node["date"];
        if (dateNode is null)
        {
            return fallback;
        }

        var s = dateNode.ToString().Trim('"');
        if (string.IsNullOrWhiteSpace(s))
        {
            return fallback;
        }

        if (DateTimeOffset.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind,
                out var dto))
        {
            return dto;
        }

        return DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt)
            ? new DateTimeOffset(dt)
            : fallback;
    }

    private static string DeductionJsonStringProp(JsonObject node, string prop)
    {
        var n = node[prop];
        if (n is null)
        {
            return "";
        }

        if (n is JsonValue jv && jv.TryGetValue<string>(out var s))
        {
            return s.Trim();
        }

        return n.ToString().Trim().Trim('"');
    }

    private static string DeductionTypeLabelAr(string? type)
    {
        var t = (type ?? "").Trim().ToLowerInvariant();
        return t switch
        {
            "penalty" => "غرامة",
            "absence" => "غياب",
            "delay" => "تأخير",
            "other" => "أخرى",
            _ => string.IsNullOrEmpty(t) ? "غير محدد" : type!.Trim()
        };
    }

    private static string BuildDeductionJournalDescription(JsonObject node, string employeeName)
    {
        var typeLabel = DeductionTypeLabelAr(DeductionJsonStringProp(node, "type"));
        var title = DeductionJsonStringProp(node, "title");
        var notes = DeductionJsonStringProp(node, "notes");

        var parts = new List<string> { "خصم موظف", $"النوع: {typeLabel}" };
        if (!string.IsNullOrWhiteSpace(title))
        {
            parts.Add($"السبب: {title}");
        }

        if (!string.IsNullOrWhiteSpace(notes))
        {
            parts.Add($"ملاحظات: {notes}");
        }

        parts.Add(employeeName);
        return string.Join(" — ", parts);
    }

    private static string BonusTypeLabelAr(string? type)
    {
        var t = (type ?? "").Trim().ToLowerInvariant();
        return t switch
        {
            "incentive" => "حافز",
            "overtime" => "إضافي",
            "achievement" => "إنجاز",
            "holiday" => "عيد",
            "other" => "أخرى",
            _ => string.IsNullOrEmpty(t) ? "غير محدد" : type!.Trim()
        };
    }

    private static string BuildBonusJournalDescription(JsonObject node, string employeeName)
    {
        var typeLabel = BonusTypeLabelAr(DeductionJsonStringProp(node, "type"));
        var title = DeductionJsonStringProp(node, "title");
        var notes = DeductionJsonStringProp(node, "notes");

        var parts = new List<string> { "مكافأة موظف", $"النوع: {typeLabel}" };
        if (!string.IsNullOrWhiteSpace(title))
        {
            parts.Add($"السبب: {title}");
        }

        if (!string.IsNullOrWhiteSpace(notes))
        {
            parts.Add($"ملاحظات: {notes}");
        }

        parts.Add(employeeName);
        return string.Join(" — ", parts);
    }

    private static DateTimeOffset ParseBonusEntryDate(JsonObject node) =>
        ParseDeductionEntryDate(node);

    /// <summary>مرجع قصير فريد (4 أحرف) لعرض التقارير؛ يُعاد المحاولة عند التعارض النادر.</summary>
    private async Task<string> GenerateUniqueJournalReference4Async(CancellationToken ct)
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var codeChars = new char[4];
        for (var attempt = 0; attempt < 100; attempt++)
        {
            for (var i = 0; i < 4; i++)
            {
                codeChars[i] = alphabet[Random.Shared.Next(alphabet.Length)];
            }

            var s = new string(codeChars);
            if (!await _db.VoucherJournalEntries.AnyAsync(x => x.Reference == s, ct))
            {
                return s;
            }
        }

        return Random.Shared.Next(1000, 10000).ToString(CultureInfo.InvariantCulture);
    }

    private async Task<string> GenerateUniqueBonusJournalReferenceAsync(CancellationToken ct)
    {
        for (var attempt = 0; attempt < 100; attempt++)
        {
            var suffix = await GenerateUniqueJournalReference4Async(ct);
            var reference = $"BN-{suffix}";
            if (!await _db.VoucherJournalEntries.AnyAsync(x => x.Reference == reference, ct))
            {
                return reference;
            }
        }

        return $"BN-{Random.Shared.Next(1000, 9999)}";
    }

    /// <summary>عملة التشغيل الافتراضية (محلي ثم رمز يمني ثم أول سجل).</summary>
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

    private static (DateTimeOffset Start, DateTimeOffset End) MonthVoucherRange(int year, int month)
    {
        var start = new DateTimeOffset(new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc));
        var end = start.AddMonths(1);
        return (start, end);
    }

    /// <summary>صافي القيود على الحساب (مدين − دائن) كما في تقرير الحسابات.</summary>
    private static decimal ComputeLedgerNetBalance(
        IEnumerable<(int? ToAccountId, int? FromAccountId, decimal Amount)> entries,
        int chartAccountId)
    {
        decimal balance = 0;
        foreach (var e in entries)
        {
            if (e.ToAccountId == chartAccountId)
            {
                balance += e.Amount;
            }

            if (e.FromAccountId == chartAccountId)
            {
                balance -= e.Amount;
            }
        }

        return balance;
    }

    private static IQueryable<VoucherJournalEntryRecord> LedgerEntriesForAccountsQuery(
        IQueryable<VoucherJournalEntryRecord> query,
        IReadOnlyList<int> chartAccountIds)
    {
        return query.Where(e =>
            (e.ToAccountId != null && chartAccountIds.Contains(e.ToAccountId.Value))
            || (e.FromAccountId != null && chartAccountIds.Contains(e.FromAccountId.Value)));
    }

    /// <summary>حركة الشهر فقط (لحساب المبلغ المستحق للصرف).</summary>
    private async Task<Dictionary<int, decimal>> SumLedgerMonthMovementByChartAccountAsync(
        IReadOnlyList<int> chartAccountIds,
        int year,
        int month,
        CancellationToken ct)
    {
        if (chartAccountIds.Count == 0)
        {
            return new Dictionary<int, decimal>();
        }

        var (start, end) = MonthVoucherRange(year, month);
        var entries = await LedgerEntriesForAccountsQuery(_db.VoucherJournalEntries.AsNoTracking(), chartAccountIds)
            .Where(e => e.EntryDate >= start && e.EntryDate < end)
            .Select(e => new { e.ToAccountId, e.FromAccountId, e.Amount })
            .ToListAsync(ct);

        return BuildLedgerBalanceMap(
            chartAccountIds,
            entries.Select(e => (e.ToAccountId, e.FromAccountId, e.Amount)));
    }

    /// <summary>رصيد تراكمي حتى نهاية الشهر (يشمل الرصيد السابق) — كتقرير «مع الرصيد السابق».</summary>
    private async Task<Dictionary<int, decimal>> SumLedgerCumulativeThroughMonthAsync(
        IReadOnlyList<int> chartAccountIds,
        int year,
        int month,
        CancellationToken ct)
    {
        if (chartAccountIds.Count == 0)
        {
            return new Dictionary<int, decimal>();
        }

        var (_, endExclusive) = MonthVoucherRange(year, month);
        var entries = await LedgerEntriesForAccountsQuery(_db.VoucherJournalEntries.AsNoTracking(), chartAccountIds)
            .Where(e => e.EntryDate < endExclusive)
            .Select(e => new { e.ToAccountId, e.FromAccountId, e.Amount })
            .ToListAsync(ct);

        return BuildLedgerBalanceMap(
            chartAccountIds,
            entries.Select(e => (e.ToAccountId, e.FromAccountId, e.Amount)));
    }

    private static Dictionary<int, decimal> BuildLedgerBalanceMap(
        IReadOnlyList<int> chartAccountIds,
        IEnumerable<(int? ToAccountId, int? FromAccountId, decimal Amount)> entries)
    {
        var result = chartAccountIds.ToDictionary(id => id, _ => 0m);
        foreach (var chartId in chartAccountIds)
        {
            result[chartId] = ComputeLedgerNetBalance(entries, chartId);
        }

        return result;
    }

    private async Task<Dictionary<int, decimal>> SumCustodyByChartAccountAsync(
        IReadOnlyList<int> chartAccountIds,
        int year,
        int month,
        CancellationToken ct)
    {
        if (chartAccountIds.Count == 0)
        {
            return new Dictionary<int, decimal>();
        }

        var (start, end) = MonthVoucherRange(year, month);
        var rows = await _db.PaymentVouchers.AsNoTracking()
            .Where(v => v.AccountId != null
                        && chartAccountIds.Contains(v.AccountId.Value)
                        && v.VoucherDate >= start
                        && v.VoucherDate < end)
            .GroupBy(v => v.AccountId!.Value)
            .Select(g => new { AccountId = g.Key, Total = g.Sum(v => v.Amount) })
            .ToListAsync(ct);

        return rows.ToDictionary(x => x.AccountId, x => x.Total);
    }

    private async Task<(decimal Custody, decimal CumulativeBalance, decimal MonthMovement)> GetCustodyAndBalanceAsync(
        EmployeeMonthlyAccountRecord account,
        int? chartAccountId,
        CancellationToken ct)
    {
        if (!chartAccountId.HasValue || chartAccountId.Value <= 0)
        {
            return (0, 0, 0);
        }

        var chartIds = new List<int> { chartAccountId.Value };
        var custodyMap = await SumCustodyByChartAccountAsync(chartIds, account.Year, account.Month, ct);
        var cumulativeMap =
            await SumLedgerCumulativeThroughMonthAsync(chartIds, account.Year, account.Month, ct);
        var monthMap = await SumLedgerMonthMovementByChartAccountAsync(chartIds, account.Year, account.Month, ct);

        var custody = custodyMap.GetValueOrDefault(chartAccountId.Value);
        var cumulative = cumulativeMap.GetValueOrDefault(chartAccountId.Value);
        var monthMovement = monthMap.GetValueOrDefault(chartAccountId.Value);
        return (custody, cumulative, monthMovement);
    }

    /// <summary>
    /// المبلغ المستحق للصرف: يعتمد على صافي الراتب ورصيد القيود والعهدة (سندات الصرف).
    /// عند «له» (رصيد سالب في الدفتر) لا يُضاف الرصيد إلى الصافي — يُؤخذ الأقل بين الاستحقاق والصافي.
    /// </summary>
    private static decimal ComputeAmountDue(decimal netSalary, decimal ledgerBalance, decimal custodyPaid)
    {
        if (netSalary <= 0)
        {
            return 0;
        }

        decimal fromLedger;
        if (ledgerBalance < 0)
        {
            fromLedger = Math.Min(netSalary, -ledgerBalance);
        }
        else if (ledgerBalance >= netSalary)
        {
            fromLedger = 0;
        }
        else
        {
            fromLedger = netSalary - ledgerBalance;
        }

        var remainingByNetAndCustody = Math.Max(0, netSalary - custodyPaid);
        return Math.Min(fromLedger, remainingByNetAndCustody);
    }

    private static void ApplyBalanceFields(
        Dictionary<string, object?> mapped,
        decimal custody,
        decimal cumulativeBalance,
        decimal monthLedgerMovement,
        decimal netSalary)
    {
        var amountDue = ComputeAmountDue(netSalary, monthLedgerMovement, custody);
        mapped["custody_amount"] = custody;
        mapped["employee_balance"] = cumulativeBalance;
        mapped["employee_balance_month"] = monthLedgerMovement;
        mapped["employee_balance_display"] =
            cumulativeBalance > 0 ? -cumulativeBalance : cumulativeBalance;
        mapped["employee_balance_side"] =
            cumulativeBalance > 0 ? "عليه" : cumulativeBalance < 0 ? "له" : "";
        // لا صرف عندما الرصيد التراكمي «عليه» (يُعرض سالباً في الواجهة)
        mapped["can_disburse"] = amountDue > 0 && cumulativeBalance <= 0;
        mapped["amount_due"] = amountDue;
    }

    private static void SyncPaidFlagsFromBalance(
        EmployeeMonthlyAccountRecord account,
        decimal custody,
        decimal monthLedgerMovement)
    {
        var amountDue = ComputeAmountDue(account.NetSalary, monthLedgerMovement, custody);
        account.IsPaid = amountDue <= 0 && custody > 0;
        account.Status = account.IsPaid
            ? "paid"
            : custody > 0 || amountDue > 0
                ? "partial"
                : "draft";
    }

    private Dictionary<string, object?> MapMonthAccount(EmployeeMonthlyAccountRecord a)
    {
        return new Dictionary<string, object?>
        {
            ["id"] = a.Id.ToString(),
            ["employee_id"] = a.EmployeeId.ToString(),
            ["employee_name"] = a.EmployeeName,
            ["year"] = a.Year,
            ["month"] = a.Month,
            ["month_name"] = a.MonthName,
            ["base_salary"] = a.BaseSalary,
            ["allowances"] = a.Allowances,
            ["deductions"] = JsonSerializer.Deserialize<object>(a.DeductionsJson ?? "[]", JsonOpts),
            ["total_deductions"] = a.TotalDeductions,
            ["bonuses"] = JsonSerializer.Deserialize<object>(a.BonusesJson ?? "[]", JsonOpts),
            ["total_bonuses"] = a.TotalBonuses,
            ["absences"] = JsonSerializer.Deserialize<object>(a.AbsencesJson ?? "[]", JsonOpts),
            ["total_absence_days"] = a.TotalAbsenceDays,
            ["absence_deduction"] = a.AbsenceDeduction,
            ["delays"] = JsonSerializer.Deserialize<object>(a.DelaysJson ?? "[]", JsonOpts),
            ["total_delay_minutes"] = a.TotalDelayMinutes,
            ["delay_deduction"] = a.DelayDeduction,
            ["extra_hours"] = JsonSerializer.Deserialize<object>(a.ExtraHoursJson ?? "[]", JsonOpts),
            ["total_extra_hours"] = a.TotalExtraHours,
            ["extra_pay"] = a.ExtraPay,
            ["attendance"] = JsonSerializer.Deserialize<object>(a.AttendanceJson ?? "[]", JsonOpts),
            ["gross_salary"] = a.GrossSalary,
            ["net_salary"] = a.NetSalary,
            ["status"] = a.Status,
            ["is_paid"] = a.IsPaid,
            ["paid_at"] = a.PaidAt,
            ["paid_by"] = a.PaidBy,
            ["payment_method"] = a.PaymentMethod,
            ["notes"] = a.Notes,
            ["created_at"] = a.CreatedAt,
            ["updated_at"] = a.UpdatedAt,
        };
    }

    private static string MonthNameAr(int month) => month switch
    {
        1 => "يناير", 2 => "فبراير", 3 => "مارس", 4 => "أبريل",
        5 => "مايو", 6 => "يونيو", 7 => "يوليو", 8 => "أغسطس",
        9 => "سبتمبر", 10 => "أكتوبر", 11 => "نوفمبر", _ => "ديسمبر",
    };

    [HttpGet("monthly-accounts")]
    public async Task<ActionResult<object>> MonthlyAccounts([FromQuery] int year, [FromQuery] int month,
        CancellationToken ct)
    {
        var rows = await _db.EmployeeMonthlyAccounts
            .AsNoTracking()
            .Where(x => x.Year == year && x.Month == month)
            .OrderBy(x => x.EmployeeName)
            .ToListAsync(ct);

        return Ok(await EnrichMonthlyAccountsAsync(rows, ct));
    }

    private async Task<List<Dictionary<string, object?>>> EnrichMonthlyAccountsAsync(
        List<EmployeeMonthlyAccountRecord> rows,
        CancellationToken ct)
    {
        if (rows.Count == 0)
        {
            return new List<Dictionary<string, object?>>();
        }

        var employeeIds = rows.Select(r => r.EmployeeId).Distinct().ToList();
        var chartByEmployee = await _db.EmployeeRecords.AsNoTracking()
            .Where(e => employeeIds.Contains(e.Id))
            .Select(e => new { e.Id, e.ChartAccountId })
            .ToDictionaryAsync(e => e.Id, e => e.ChartAccountId, ct);

        var chartIds = chartByEmployee.Values
            .Where(c => c.HasValue && c.Value > 0)
            .Select(c => c!.Value)
            .Distinct()
            .ToList();

        var year = rows[0].Year;
        var month = rows[0].Month;
        var custodyByChart = await SumCustodyByChartAccountAsync(chartIds, year, month, ct);
        var cumulativeByChart = await SumLedgerCumulativeThroughMonthAsync(chartIds, year, month, ct);
        var monthMovementByChart = await SumLedgerMonthMovementByChartAccountAsync(chartIds, year, month, ct);

        var result = new List<Dictionary<string, object?>>(rows.Count);
        foreach (var row in rows)
        {
            var mapped = MapMonthAccount(row);
            var chartId = chartByEmployee.GetValueOrDefault(row.EmployeeId);
            var custody = chartId is > 0 ? custodyByChart.GetValueOrDefault(chartId.Value) : 0m;
            var cumulative = chartId is > 0 ? cumulativeByChart.GetValueOrDefault(chartId.Value) : 0m;
            var monthMovement = chartId is > 0 ? monthMovementByChart.GetValueOrDefault(chartId.Value) : 0m;
            ApplyBalanceFields(mapped, custody, cumulative, monthMovement, row.NetSalary);
            if (chartId is > 0)
            {
                mapped["chart_account_id"] = chartId.Value;
            }

            result.Add(mapped);
        }

        return result;
    }

    [HttpGet("monthly-accounts/by-employee")]
    public async Task<ActionResult<object>> MonthlyAccountByEmployee(
        [FromQuery(Name = "employee_id")] string employeeId,
        [FromQuery] int year,
        [FromQuery] int month,
        CancellationToken ct)
    {
        if (!Guid.TryParse(employeeId, out var empGuid))
        {
            return BadRequest(new { message = "معرف الموظف غير صالح." });
        }

        var row = await _db.EmployeeMonthlyAccounts.AsNoTracking()
            .FirstOrDefaultAsync(x => x.EmployeeId == empGuid && x.Year == year && x.Month == month, ct);
        if (row is null)
        {
            return NotFound();
        }

        var enriched = await EnrichMonthlyAccountsAsync(new List<EmployeeMonthlyAccountRecord> { row }, ct);
        return Ok(enriched[0]);
    }

    public class MonthlyAccountCreateBody
    {
        [JsonPropertyName("employee_id")]
        public string EmployeeId { get; set; } = "";

        public int Year { get; set; }

        public int Month { get; set; }
    }

    [HttpPost("monthly-accounts")]
    public async Task<ActionResult<object>> CreateMonthlyAccount([FromBody] MonthlyAccountCreateBody body,
        CancellationToken ct)
    {
        if (!Guid.TryParse(body.EmployeeId, out var employeeId))
        {
            return BadRequest(new { message = "معرف الموظف غير صالح." });
        }

        var employee = await _db.EmployeeRecords.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == employeeId && e.Status == "active", ct);
        if (employee is null)
        {
            return NotFound(new { message = "الموظف غير موجود أو غير نشط." });
        }

        if (!employee.ChartAccountId.HasValue || employee.ChartAccountId.Value <= 0)
        {
            return BadRequest(new { message = "يجب ربط الموظف بحساب محاسبي فرعي من دليل الحسابات قبل فتح حساب الراتب الشهري." });
        }

        var dup = await _db.EmployeeMonthlyAccounts
            .AnyAsync(x => x.EmployeeId == employeeId && x.Year == body.Year && x.Month == body.Month, ct);
        if (dup)
        {
            return Conflict(new { message = "حساب هذا الموظف موجود مسبقاً لهذا الشهر." });
        }

        var now = DateTimeOffset.UtcNow;
        var gross = employee.BaseSalary + employee.Allowances;

        var acc = new EmployeeMonthlyAccountRecord
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            EmployeeName = employee.Name,
            Year = body.Year,
            Month = body.Month,
            MonthName = MonthNameAr(body.Month),
            BaseSalary = employee.BaseSalary,
            Allowances = employee.Allowances,
            DeductionsJson = "[]",
            BonusesJson = "[]",
            AttendanceJson = "[]",
            AbsencesJson = "[]",
            DelaysJson = "[]",
            ExtraHoursJson = "[]",
            GrossSalary = gross,
            NetSalary = gross,
            Status = "draft",
            IsPaid = false,
            CreatedAt = now,
            UpdatedAt = now,
        };

        _db.EmployeeMonthlyAccounts.Add(acc);
        await _db.SaveChangesAsync(ct);
        var created = await EnrichMonthlyAccountsAsync(new List<EmployeeMonthlyAccountRecord> { acc }, ct);
        return Ok(created[0]);
    }

    [HttpPost("monthly-upsert")]
    public async Task<ActionResult<object>> UpsertMonthly([FromBody] MonthlyAccountCreateBody body, CancellationToken ct)
    {
        if (!Guid.TryParse(body.EmployeeId, out var employeeId))
        {
            return BadRequest(new { message = "معرف الموظف غير صالح." });
        }

        var employee = await _db.EmployeeRecords.FirstOrDefaultAsync(e => e.Id == employeeId, ct);
        if (employee is null)
        {
            return NotFound(new { message = "الموظف غير موجود." });
        }

        var existing = await _db.EmployeeMonthlyAccounts
            .FirstOrDefaultAsync(x => x.EmployeeId == employeeId && x.Year == body.Year && x.Month == body.Month, ct);

        var now = DateTimeOffset.UtcNow;
        var gross = employee.BaseSalary + employee.Allowances;

        if (existing is null)
        {
            if (!employee.ChartAccountId.HasValue || employee.ChartAccountId.Value <= 0)
            {
                return BadRequest(new { message = "يجب ربط الموظف بحساب محاسبي فرعي قبل إنشاء حساب الراتب لهذا الشهر." });
            }

            var acc = new EmployeeMonthlyAccountRecord
            {
                Id = Guid.NewGuid(),
                EmployeeId = employeeId,
                EmployeeName = employee.Name,
                Year = body.Year,
                Month = body.Month,
                MonthName = MonthNameAr(body.Month),
                BaseSalary = employee.BaseSalary,
                Allowances = employee.Allowances,
                DeductionsJson = "[]",
                BonusesJson = "[]",
                AttendanceJson = "[]",
                AbsencesJson = "[]",
                DelaysJson = "[]",
                ExtraHoursJson = "[]",
                GrossSalary = gross,
                NetSalary = gross,
                Status = "draft",
                IsPaid = false,
                CreatedAt = now,
                UpdatedAt = now,
            };
            _db.EmployeeMonthlyAccounts.Add(acc);
            await _db.SaveChangesAsync(ct);
            var inserted = await EnrichMonthlyAccountsAsync(new List<EmployeeMonthlyAccountRecord> { acc }, ct);
            return Ok(inserted[0]);
        }

        if (!existing.IsPaid &&
            (existing.BaseSalary != employee.BaseSalary || existing.Allowances != employee.Allowances))
        {
            existing.BaseSalary = employee.BaseSalary;
            existing.Allowances = employee.Allowances;
            existing.EmployeeName = employee.Name;
            existing.GrossSalary = gross;

            var totalDeductions =
                SumAmounts(ParseArr(existing.DeductionsJson)?.AsArray() ?? new JsonArray());
            existing.TotalDeductions = totalDeductions;

            var totalBonuses =
                SumAmounts(ParseArr(existing.BonusesJson)?.AsArray() ?? new JsonArray());
            existing.TotalBonuses = totalBonuses;

            existing.NetSalary = RecalcNet(existing.GrossSalary, existing.TotalDeductions, existing.TotalBonuses);
        }

        existing.UpdatedAt = now;
        await _db.SaveChangesAsync(ct);
        var updated = await EnrichMonthlyAccountsAsync(new List<EmployeeMonthlyAccountRecord> { existing }, ct);
        return Ok(updated[0]);
    }

    [HttpGet("employees-without-account")]
    public async Task<ActionResult<object>> EmployeesWithoutAccount([FromQuery] int year, [FromQuery] int month,
        CancellationToken ct)
    {
        var activeIds =
            await _db.EmployeeRecords.AsNoTracking()
                .Where(e => e.Status == "active").Select(e => e.Id).ToListAsync(ct);
        var withAcc = await _db.EmployeeMonthlyAccounts.AsNoTracking()
            .Where(x => x.Year == year && x.Month == month)
            .Select(x => x.EmployeeId)
            .ToListAsync(ct);

        var missing = activeIds.Where(id => !withAcc.Contains(id)).ToList();
        var employees = await _db.EmployeeRecords.AsNoTracking()
            .Where(e => missing.Contains(e.Id) && e.ChartAccountId != null && e.ChartAccountId > 0)
            .OrderBy(e => e.Name)
            .ToListAsync(ct);

        return Ok(employees.Select(EmployeesController.MapEmployeeProjection));
    }

    /// <summary>يستبدل قائمة الخصومات بالكامل (مع إعادة حساب الإجمالي وصافي الراتب).</summary>
    [HttpPut("monthly-accounts/{id:guid}/deductions")]
    public async Task<ActionResult<object>> SetDeductions(Guid id, [FromBody] JsonElement bodyElement, CancellationToken ct)
    {
        var account = await _db.EmployeeMonthlyAccounts.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (account is null)
        {
            return NotFound();
        }

        if (bodyElement.ValueKind != JsonValueKind.Array)
        {
            return BadRequest(new { message = "يجب أن تكون الخصومات مصفوفة JSON." });
        }

        account.DeductionsJson = bodyElement.GetRawText();

        var arr = ParseArr(account.DeductionsJson)?.AsArray() ?? new JsonArray();
        account.TotalDeductions = SumAmounts(arr);

        account.NetSalary =
            RecalcNet(account.GrossSalary, account.TotalDeductions, account.TotalBonuses);
        account.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);

        var employeeAfterSet = await _db.EmployeeRecords.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == account.EmployeeId, ct);
        var (custodySet, cumulativeSet, monthSet) =
            await GetCustodyAndBalanceAsync(account, employeeAfterSet?.ChartAccountId, ct);
        SyncPaidFlagsFromBalance(account, custodySet, monthSet);
        await _db.SaveChangesAsync(ct);

        var mappedSet = MapMonthAccount(account);
        ApplyBalanceFields(mappedSet, custodySet, cumulativeSet, monthSet, account.NetSalary);
        return Ok(mappedSet);
    }

    [HttpPost("monthly-accounts/{id:guid}/deductions")]
    public async Task<ActionResult<object>> AddDeduction(Guid id, [FromBody] JsonElement bodyElement, CancellationToken ct)
    {
        var account = await _db.EmployeeMonthlyAccounts.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (account is null)
        {
            return NotFound();
        }

        var node = JsonNode.Parse(bodyElement.GetRawText()) as JsonObject
                   ?? JsonSerializer.Deserialize<JsonObject>(bodyElement.GetRawText(), JsonOpts);
        if (node is null)
        {
            return BadRequest(new { message = "بيانات الخصم غير صالحة." });
        }

        if (node["id"] is null || string.IsNullOrWhiteSpace(node["id"]!.ToString()))
        {
            node["id"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
        }

        var deductionId = node["id"]!.ToString();

        var amtNode = node["amount"];
        var amtStr = amtNode?.ToString();
        if (string.IsNullOrEmpty(amtStr) ||
            !decimal.TryParse(amtStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var deductionAmount) ||
            deductionAmount <= 0)
        {
            return BadRequest(new { message = "مبلغ الخصم يجب أن يكون أكبر من صفر." });
        }

        var employee = await _db.EmployeeRecords.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == account.EmployeeId, ct);
        if (employee is null)
        {
            return BadRequest(new { message = "الموظف غير موجود لهذا الحساب الشهري." });
        }

        if (!employee.ChartAccountId.HasValue || employee.ChartAccountId.Value <= 0)
        {
            return BadRequest(new { message = "يجب ربط الموظف بحساب محاسبي فرعي قبل إضافة الخصومات." });
        }

        var transitSettings = await _db.TransitAccountsSettings.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == 1, ct);
        var transitAccountId = transitSettings?.CourierCommissionAccount;
        if (!transitAccountId.HasValue || transitAccountId.Value <= 0)
        {
            return BadRequest(new { message = "حدّد حساب وسيط خصومات الموظفين من صفحة الحسابات الوسيطة أولاً." });
        }

        var arr = ParseArr(account.DeductionsJson)?.AsArray() ?? new JsonArray();
        foreach (var existing in arr)
        {
            if (existing is JsonObject jo && jo["id"]?.ToString() == deductionId)
            {
                return Conflict(new { message = "معرّف الخصم مستخدم مسبقاً في هذا الشهر." });
            }
        }

        arr.Add(node);
        account.DeductionsJson = arr.ToJsonString(JsonOpts);

        account.TotalDeductions = SumAmounts(arr);

        account.NetSalary =
            RecalcNet(account.GrossSalary, account.TotalDeductions, account.TotalBonuses);
        account.UpdatedAt = DateTimeOffset.UtcNow;

        var entryNumber = await NextJournalEntryNumberAsync(ct);
        var desc = BuildDeductionJournalDescription(node, employee.Name);
        var reference = await GenerateUniqueJournalReference4Async(ct);
        var payrollCurrencyId = await ResolveDefaultOperationalCurrencyIdAsync(ct);

        var now = DateTimeOffset.UtcNow;
        // خصم على الموظف: الموظف «عليه» (to)، الحساب الوسيط «له» (from) — يطابق عرض القيود في الواجهة.
        _db.VoucherJournalEntries.Add(new VoucherJournalEntryRecord
        {
            Id = Guid.NewGuid(),
            EntryNumber = entryNumber,
            EntryDate = ParseDeductionEntryDate(node),
            Description = desc,
            FromAccountId = transitAccountId.Value,
            ToAccountId = employee.ChartAccountId.Value,
            CurrencyId = payrollCurrencyId,
            Amount = deductionAmount,
            Reference = reference,
            CreatedAt = now,
            PostedAt = now,
        });

        var (custodyDed, cumulativeDed, monthDed) =
            await GetCustodyAndBalanceAsync(account, employee.ChartAccountId, ct);
        SyncPaidFlagsFromBalance(account, custodyDed, monthDed);
        await _db.SaveChangesAsync(ct);

        var mappedDed = MapMonthAccount(account);
        ApplyBalanceFields(mappedDed, custodyDed, cumulativeDed, monthDed, account.NetSalary);
        return Ok(mappedDed);
    }

    [HttpPost("monthly-accounts/{id:guid}/bonuses")]
    public async Task<ActionResult<object>> AddBonus(Guid id, [FromBody] JsonElement bodyElement, CancellationToken ct)
    {
        var account = await _db.EmployeeMonthlyAccounts.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (account is null)
        {
            return NotFound();
        }

        var node = JsonNode.Parse(bodyElement.GetRawText()) as JsonObject
                   ?? JsonSerializer.Deserialize<JsonObject>(bodyElement.GetRawText(), JsonOpts);
        if (node is null)
        {
            return BadRequest(new { message = "بيانات المكافأة غير صالحة." });
        }

        if (node["id"] is null || string.IsNullOrWhiteSpace(node["id"]!.ToString()))
        {
            node["id"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
        }

        var bonusId = node["id"]!.ToString();

        var amtNode = node["amount"];
        var amtStr = amtNode?.ToString();
        if (string.IsNullOrEmpty(amtStr) ||
            !decimal.TryParse(amtStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var bonusAmount) ||
            bonusAmount <= 0)
        {
            return BadRequest(new { message = "مبلغ المكافأة يجب أن يكون أكبر من صفر." });
        }

        var employee = await _db.EmployeeRecords.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == account.EmployeeId, ct);
        if (employee is null)
        {
            return BadRequest(new { message = "الموظف غير موجود لهذا الحساب الشهري." });
        }

        if (!employee.ChartAccountId.HasValue || employee.ChartAccountId.Value <= 0)
        {
            return BadRequest(new { message = "يجب ربط الموظف بحساب محاسبي فرعي قبل إضافة المكافآت." });
        }

        var transitSettings = await _db.TransitAccountsSettings.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == 1, ct);
        var transitAccountId = transitSettings?.TransferGuaranteeAccount;
        if (!transitAccountId.HasValue || transitAccountId.Value <= 0)
        {
            return BadRequest(new { message = "حدّد حساب وسيط مكافآت الموظفين من صفحة الحسابات الوسيطة أولاً." });
        }

        var arr = ParseArr(account.BonusesJson)?.AsArray() ?? new JsonArray();
        foreach (var existing in arr)
        {
            if (existing is JsonObject jo && jo["id"]?.ToString() == bonusId)
            {
                return Conflict(new { message = "معرّف المكافأة مستخدم مسبقاً في هذا الشهر." });
            }
        }

        var entryNumber = await NextJournalEntryNumberAsync(ct);
        var desc = BuildBonusJournalDescription(node, employee.Name);
        var reference = await GenerateUniqueBonusJournalReferenceAsync(ct);
        var payrollCurrencyId = await ResolveDefaultOperationalCurrencyIdAsync(ct);
        var now = DateTimeOffset.UtcNow;

        node["journal_reference"] = reference;
        arr.Add(node);
        account.BonusesJson = arr.ToJsonString(JsonOpts);

        account.TotalBonuses = SumAmounts(arr);
        account.NetSalary =
            RecalcNet(account.GrossSalary, account.TotalDeductions, account.TotalBonuses);
        account.UpdatedAt = DateTimeOffset.UtcNow;

        // مكافأة: الموظف «له» (from)، الوسيط «عليه» (to) — عكس الخصم.
        _db.VoucherJournalEntries.Add(new VoucherJournalEntryRecord
        {
            Id = Guid.NewGuid(),
            EntryNumber = entryNumber,
            EntryDate = ParseBonusEntryDate(node),
            Description = desc,
            FromAccountId = employee.ChartAccountId.Value,
            ToAccountId = transitAccountId.Value,
            CurrencyId = payrollCurrencyId,
            Amount = bonusAmount,
            Reference = reference,
            CreatedAt = now,
            PostedAt = now,
        });

        var (custodyBon, cumulativeBon, monthBon) =
            await GetCustodyAndBalanceAsync(account, employee.ChartAccountId, ct);
        SyncPaidFlagsFromBalance(account, custodyBon, monthBon);
        await _db.SaveChangesAsync(ct);

        var mappedBon = MapMonthAccount(account);
        ApplyBalanceFields(mappedBon, custodyBon, cumulativeBon, monthBon, account.NetSalary);
        return Ok(mappedBon);
    }

    [HttpPut("monthly-accounts/{id:guid}/attendance")]
    public async Task<IActionResult> UpdateAttendance(Guid id, [FromBody] JsonElement attendanceArray, CancellationToken ct)
    {
        var account = await _db.EmployeeMonthlyAccounts.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (account is null)
        {
            return NotFound();
        }

        if (attendanceArray.ValueKind != JsonValueKind.Array)
        {
            return BadRequest(new { message = "الحضور يجب أن يكون مصفوفة." });
        }

        account.AttendanceJson = attendanceArray.GetRawText();
        account.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    public class PayBody
    {
        [JsonPropertyName("paid_by")]
        public string PaidBy { get; set; } = "";

        [JsonPropertyName("payment_method")]
        public string PaymentMethod { get; set; } = "";

        [JsonPropertyName("cash_box_id")]
        public int? CashBoxId { get; set; }

        [JsonPropertyName("bank_id")]
        public int? BankId { get; set; }

        [JsonPropertyName("transfer_no")]
        public string? TransferNo { get; set; }

        [JsonPropertyName("notes")]
        public string Notes { get; set; } = "";
    }

    private static string NormalizeSalaryPaymentType(string? method)
    {
        var m = (method ?? "").Trim().ToLowerInvariant();
        if (m is "cash" or "نقدي" or "نقد")
        {
            return "cash";
        }

        if (m is "bank" or "بنك" or "تحويل بنكي" or "تحويل")
        {
            return "bank";
        }

        return "";
    }

    private static string SalaryPaymentMethodLabel(string paymentType) =>
        paymentType == "bank" ? "تحويل بنكي" : "نقدي";

    private async Task<int> NextPaymentVoucherIdAsync(CancellationToken ct)
    {
        var max = await _db.PaymentVouchers.Select(x => (int?)x.Id).MaxAsync(ct) ?? 0;
        return max + 1;
    }

    private async Task<string> NextPaymentVoucherNoAsync(CancellationToken ct)
    {
        var numbers = await _db.PaymentVouchers.AsNoTracking()
            .Select(x => x.VoucherNo)
            .ToListAsync(ct);
        var max = 0;
        foreach (var no in numbers)
        {
            if (int.TryParse((no ?? "").Trim(), out var n) && n > max)
            {
                max = n;
            }
        }

        return (max + 1).ToString(CultureInfo.InvariantCulture);
    }

    [HttpPatch("monthly-accounts/{id:guid}/pay")]
    public async Task<ActionResult<object>> MarkPaid(Guid id, [FromBody] PayBody body, CancellationToken ct)
    {
        var account = await _db.EmployeeMonthlyAccounts.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (account is null)
        {
            return NotFound();
        }

        var employee = await _db.EmployeeRecords.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == account.EmployeeId, ct);
        if (employee is null)
        {
            return BadRequest(new { message = "الموظف غير موجود لهذا الحساب الشهري." });
        }

        if (!employee.ChartAccountId.HasValue || employee.ChartAccountId.Value <= 0)
        {
            return BadRequest(new { message = "يجب ربط الموظف بحساب محاسبي فرعي قبل صرف الراتب." });
        }

        var (custodyBefore, cumulativeBefore, monthBefore) =
            await GetCustodyAndBalanceAsync(account, employee.ChartAccountId, ct);
        if (cumulativeBefore > 0)
        {
            return BadRequest(new { message = "لا يمكن الصرف: رصيد الحساب عليه (يظهر سالباً)." });
        }

        var payAmount = ComputeAmountDue(account.NetSalary, monthBefore, custodyBefore);
        if (payAmount <= 0)
        {
            return BadRequest(new { message = "لا يوجد مبلغ مستحق للصرف." });
        }

        var paymentType = NormalizeSalaryPaymentType(body.PaymentMethod);
        if (paymentType is not ("cash" or "bank"))
        {
            return BadRequest(new { message = "اختر طريقة دفع صالحة (نقدي أو بنك)." });
        }

        int? sourceChartAccountId;
        int? cashBoxId = null;
        int? bankId = null;
        if (paymentType == "cash")
        {
            if (!body.CashBoxId.HasValue || body.CashBoxId.Value <= 0)
            {
                return BadRequest(new { message = "اختر الصندوق." });
            }

            cashBoxId = body.CashBoxId.Value;
            sourceChartAccountId =
                await AccountingVoucherAccountResolver.ResolveCashBoxChartAccountIdAsync(_db, cashBoxId.Value, ct);
            if (!sourceChartAccountId.HasValue || sourceChartAccountId.Value <= 0)
            {
                return BadRequest(new { message = "الصندوق غير مرتبط بحساب محاسبي." });
            }
        }
        else
        {
            if (!body.BankId.HasValue || body.BankId.Value <= 0)
            {
                return BadRequest(new { message = "اختر البنك." });
            }

            bankId = body.BankId.Value;
            sourceChartAccountId =
                await AccountingVoucherAccountResolver.ResolveBankChartAccountIdAsync(_db, bankId.Value, ct);
            if (!sourceChartAccountId.HasValue || sourceChartAccountId.Value <= 0)
            {
                return BadRequest(new { message = "البنك غير مرتبط بحساب محاسبي." });
            }
        }

        var currencyId = await ResolveDefaultOperationalCurrencyIdAsync(ct);
        if (!currencyId.HasValue || currencyId.Value <= 0)
        {
            return BadRequest(new { message = "لم يتم العثور على عملة افتراضية للسند." });
        }

        var voucherId = await NextPaymentVoucherIdAsync(ct);
        var journalRef = $"PV-{voucherId}";
        if (await _db.VoucherJournalEntries.AnyAsync(x => x.Reference == journalRef, ct))
        {
            return Conflict(new { message = "سند الصرف موجود مسبقاً لهذا الرقم." });
        }

        var now = DateTimeOffset.UtcNow;
        var notes = (body.Notes ?? "").Trim();
        var defaultDesc =
            $"صرف راتب {employee.Name} — {account.MonthName} {account.Year}";
        var journalDesc = string.IsNullOrEmpty(notes) ? defaultDesc : notes;
        var voucherNo = await NextPaymentVoucherNoAsync(ct);

        var voucher = new PaymentVoucherRecord
        {
            Id = voucherId,
            VoucherNo = voucherNo,
            VoucherDate = now,
            PaymentType = paymentType,
            CashBoxAccountId = cashBoxId,
            BankAccountId = bankId,
            TransferNo = (body.TransferNo ?? "").Trim(),
            CurrencyId = currencyId,
            Amount = payAmount,
            AccountId = employee.ChartAccountId.Value,
            Notes = journalDesc,
            CreatedBy = 1,
            BranchId = 1,
            CreatedAt = now,
        };
        _db.PaymentVouchers.Add(voucher);

        var entryNumber = await NextJournalEntryNumberAsync(ct);
        _db.VoucherJournalEntries.Add(new VoucherJournalEntryRecord
        {
            Id = Guid.NewGuid(),
            EntryNumber = entryNumber,
            EntryDate = now,
            Description = journalDesc,
            FromAccountId = sourceChartAccountId.Value,
            ToAccountId = employee.ChartAccountId.Value,
            CurrencyId = currencyId,
            Amount = payAmount,
            Reference = journalRef,
            CreatedBy = 1,
            BranchId = 1,
            CreatedAt = now,
            PostedAt = now,
        });

        account.PaidAt = now;
        account.PaidBy = body.PaidBy;
        account.PaymentMethod = SalaryPaymentMethodLabel(paymentType);
        account.UpdatedAt = now;

        await _db.SaveChangesAsync(ct);

        var (custodyAfter, cumulativeAfter, monthAfter) =
            await GetCustodyAndBalanceAsync(account, employee.ChartAccountId, ct);
        SyncPaidFlagsFromBalance(account, custodyAfter, monthAfter);
        await _db.SaveChangesAsync(ct);

        var mapped = MapMonthAccount(account);
        ApplyBalanceFields(mapped, custodyAfter, cumulativeAfter, monthAfter, account.NetSalary);
        mapped["payment_voucher_id"] = voucherId;
        mapped["payment_voucher_no"] = voucherNo;
        mapped["journal_reference"] = journalRef;
        mapped["disbursed_amount"] = payAmount;
        return Ok(mapped);
    }

    [HttpGet("absence-settings")]
    public async Task<ActionResult<object>> GetAbsence([FromQuery] int year, [FromQuery] int month, CancellationToken ct)
    {
        var row = await _db.EmployeeAbsenceSettings.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Year == year && x.Month == month, ct);
        if (row is null)
        {
            return Ok(new { deduction_with_excuse = 10m, deduction_without_excuse = 20m });
        }

        return Ok(new
        {
            deduction_with_excuse = row.DeductionWithExcuse,
            deduction_without_excuse = row.DeductionWithoutExcuse,
        });
    }

    public class AbsenceUpsertBody
    {
        public int Year { get; set; }
        public int Month { get; set; }

        [JsonPropertyName("deduction_with_excuse")]
        public decimal DeductionWithExcuse { get; set; }

        [JsonPropertyName("deduction_without_excuse")]
        public decimal DeductionWithoutExcuse { get; set; }
    }

    [HttpPut("absence-settings")]
    public async Task<IActionResult> UpsertAbsence([FromBody] AbsenceUpsertBody body, CancellationToken ct)
    {
        var row = await _db.EmployeeAbsenceSettings.FirstOrDefaultAsync(x => x.Year == body.Year && x.Month == body.Month, ct);

        var now = DateTimeOffset.UtcNow;
        if (row is null)
        {
            _db.EmployeeAbsenceSettings.Add(new EmployeeAbsenceSettingRecord
            {
                Year = body.Year,
                Month = body.Month,
                DeductionWithExcuse = body.DeductionWithExcuse,
                DeductionWithoutExcuse = body.DeductionWithoutExcuse,
                UpdatedAt = now,
            });
        }
        else
        {
            row.DeductionWithExcuse = body.DeductionWithExcuse;
            row.DeductionWithoutExcuse = body.DeductionWithoutExcuse;
            row.UpdatedAt = now;
        }

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpGet("monthly-process")]
    public async Task<ActionResult<object?>> MonthlyProcess([FromQuery] int year, [FromQuery] int month,
        CancellationToken ct)
    {
        var row = await _db.EmployeeMonthlyProcesses.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Year == year && x.Month == month, ct);
        if (row is null) return Ok(null);

        return Ok(new
        {
            id = row.Id.ToString(),
            year = row.Year,
            month = row.Month,
            month_name = row.MonthName,
            start_date = row.StartDate,
            end_date = row.EndDate,
            status = row.Status,
            created_at = row.CreatedAt,
            completed_at = row.CompletedAt,
        });
    }

    public class MonthlyProcessStartBody
    {
        public int Year { get; set; }
        public int Month { get; set; }
    }

    [HttpPost("monthly-process/start")]
    public async Task<ActionResult<object>> MonthlyProcessStart([FromBody] MonthlyProcessStartBody body,
        CancellationToken ct)
    {
        var exists = await _db.EmployeeMonthlyProcesses
            .FirstOrDefaultAsync(x => x.Year == body.Year && x.Month == body.Month, ct);

        var now = DateTimeOffset.UtcNow;
        EmployeeMonthlyProcessRecord row;
        if (exists is null)
        {
            row = new EmployeeMonthlyProcessRecord
            {
                Id = Guid.NewGuid(),
                Year = body.Year,
                Month = body.Month,
                MonthName = MonthNameAr(body.Month),
                StartDate = new DateTime(body.Year, body.Month, 1),
                EndDate = new DateTime(body.Year, body.Month, DateTime.DaysInMonth(body.Year, body.Month)),
                Status = "processing",
                CreatedAt = now,
            };
            _db.EmployeeMonthlyProcesses.Add(row);
        }
        else
        {
            row = exists;
            row.Status = "processing";
            row.CreatedAt ??= now;
        }

        await _db.SaveChangesAsync(ct);

        return Ok(new
        {
            id = row.Id.ToString(),
            year = row.Year,
            month = row.Month,
            month_name = row.MonthName,
            start_date = row.StartDate,
            end_date = row.EndDate,
            status = row.Status,
            created_at = row.CreatedAt,
            completed_at = row.CompletedAt,
        });
    }

    [HttpGet("paid-accounts")]
    public async Task<ActionResult<object>> PaidAccounts(CancellationToken ct)
    {
        var rows = await _db.EmployeeMonthlyAccounts.AsNoTracking()
            .Where(x => x.IsPaid)
            .OrderByDescending(x => x.PaidAt)
            .ToListAsync(ct);

        var projected = rows.Select(MapMonthAccount).ToList();
        DateTimeOffset lastUpdate = rows.Count == 0
            ? DateTimeOffset.UtcNow
            : rows.Select(x => x.UpdatedAt ?? x.PaidAt ?? DateTimeOffset.MinValue).Max();

        return Ok(new { data = projected, last_update = lastUpdate });
    }

    [HttpGet("paid-accounts/check-update")]
    public async Task<ActionResult<object>> PaidAccountsCheckSince([FromQuery] DateTimeOffset since,
        CancellationToken ct)
    {
        var exists = await _db.EmployeeMonthlyAccounts.AsNoTracking()
            .AnyAsync(x =>
                x.IsPaid && ((x.UpdatedAt ?? x.PaidAt) != null &&
                             (x.UpdatedAt ?? x.PaidAt)!.Value > since), ct);
        return Ok(new { has_updates = exists });
    }

    /// <summary>لشاشة الغرفة: خصومات الموظفين — فلترة بتاريخ تقويمي yyyy-MM-dd.</summary>
    [HttpGet("movements/deductions")]
    public async Task<ActionResult<List<Dictionary<string, object?>>>> DeductionMovements(
        [FromQuery] string start,
        [FromQuery] string end,
        CancellationToken ct)
    {
        if (!DateOnly.TryParseExact(start, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var startDay) ||
            !DateOnly.TryParseExact(end, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var endDay))
        {
            return BadRequest(new { message = "استخدم start وend بصيغة yyyy-MM-dd." });
        }

        if (endDay < startDay)
        {
            return BadRequest(new { message = "نطاق التاريخ غير صالح." });
        }

        var rows = await _db.EmployeeMonthlyAccounts.AsNoTracking().ToListAsync(ct);
        var list = new List<Dictionary<string, object?>>();
        foreach (var acc in rows)
        {
            foreach (var node in ParseArr(acc.DeductionsJson)?.AsArray() ?? new JsonArray())
            {
                if (node is null) continue;
                if (!TryParseMovementCalendarDate(node["date"], out var d)) continue;
                if (d < startDay || d > endDay) continue;

                list.Add(new Dictionary<string, object?>
                {
                    ["id"] = node["id"]?.ToString(),
                    ["type"] = node["type"]?.ToString(),
                    ["title"] = node["title"]?.ToString(),
                    ["amount"] = ParseMoney(node["amount"]),
                    ["date"] = d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    ["notes"] = node["notes"]?.ToString(),
                    ["created_by"] = node["created_by"]?.ToString(),
                    ["employee_id"] = acc.EmployeeId.ToString(),
                    ["employee_name"] = acc.EmployeeName,
                    ["account_id"] = acc.Id.ToString(),
                    ["type_detail"] = "deduction",
                });
            }
        }

        return Ok(list);
    }

    [HttpGet("movements/bonuses")]
    public async Task<ActionResult<List<Dictionary<string, object?>>>> BonusMovements(
        [FromQuery] string start,
        [FromQuery] string end,
        CancellationToken ct)
    {
        if (!DateOnly.TryParseExact(start, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var startDay) ||
            !DateOnly.TryParseExact(end, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var endDay))
        {
            return BadRequest(new { message = "استخدم start وend بصيغة yyyy-MM-dd." });
        }

        if (endDay < startDay)
        {
            return BadRequest(new { message = "نطاق التاريخ غير صالح." });
        }

        var rows = await _db.EmployeeMonthlyAccounts.AsNoTracking().ToListAsync(ct);
        var list = new List<Dictionary<string, object?>>();
        foreach (var acc in rows)
        {
            foreach (var node in ParseArr(acc.BonusesJson)?.AsArray() ?? new JsonArray())
            {
                if (node is null) continue;
                if (!TryParseMovementCalendarDate(node["date"], out var d)) continue;
                if (d < startDay || d > endDay) continue;

                list.Add(new Dictionary<string, object?>
                {
                    ["id"] = node["id"]?.ToString(),
                    ["type"] = node["type"]?.ToString(),
                    ["title"] = node["title"]?.ToString(),
                    ["amount"] = ParseMoney(node["amount"]),
                    ["date"] = d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    ["notes"] = node["notes"]?.ToString(),
                    ["created_by"] = node["created_by"]?.ToString(),
                    ["employee_id"] = acc.EmployeeId.ToString(),
                    ["employee_name"] = acc.EmployeeName,
                    ["account_id"] = acc.Id.ToString(),
                    ["type_detail"] = "bonus",
                });
            }
        }

        return Ok(list);
    }

    private static decimal ParseMoney(JsonNode? n)
    {
        if (n is null) return 0;
        var s = n.ToString();
        return decimal.TryParse(s, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var dec)
            ? dec
            : 0;
    }

    /// <summary>
    /// يوم الحركة بتقويم yyyy-MM-dd: يُفضَّل ذلك في JSON؛ وأيًا من ISO الزمني القديم يُحوَّل بتوقيت +03:00.
    /// </summary>
    private static bool TryParseMovementCalendarDate(JsonNode? n, out DateOnly day)
    {
        day = default;
        if (n is null) return false;
        var s = n.ToString()?.Trim();
        if (string.IsNullOrWhiteSpace(s)) return false;

        if (s.Length >= 10 && s[4] == '-' && s[7] == '-')
        {
            var head = s[..10];
            if (DateOnly.TryParseExact(head, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out day))
            {
                return true;
            }
        }

        if (DateTimeOffset.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dto))
        {
            var riyadh = dto.ToOffset(TimeSpan.FromHours(3));
            day = DateOnly.FromDateTime(riyadh.DateTime);
            return true;
        }

        return false;
    }
}
