using Microsoft.EntityFrameworkCore;
using SchoolsManagement.Api.Data;
using SchoolsManagement.Api.Models.Accounting;

namespace SchoolsManagement.Api.Services;

/// <summary>ترحيل قيود رواتب (SAL-*) فقط — تلقائياً في آخر يوم من الشهر.</summary>
public static class SalaryJournalPostingService
{
    public const string ReferencePrefix = "SAL-";

    public static bool IsSalaryAccrualReference(string? reference) =>
        !string.IsNullOrEmpty(reference)
        && reference.StartsWith(ReferencePrefix, StringComparison.Ordinal);

    public static bool IsLastDayOfMonth(DateTime date) =>
        date.Day == DateTime.DaysInMonth(date.Year, date.Month);

    /// <summary>آخر يوم من الشهر — منتصف النهار بتوقيت اليمن (UTC+3) حتى لا يظهر التاريخ يوماً تالياً عند العرض.</summary>
    public static DateTimeOffset LastDayOfMonthEntryDate(int year, int month)
    {
        var lastDay = DateTime.DaysInMonth(year, month);
        return new DateTimeOffset(year, month, lastDay, 12, 0, 0, TimeSpan.FromHours(3));
    }

    /// <returns>عدد القيود التي رُحِّلت</returns>
    public static async Task<int> PostPendingSalaryAccrualsAsync(
        ApplicationDbContext db,
        DateTimeOffset postedAt,
        CancellationToken ct)
    {
        var pending = await db.VoucherJournalEntries
            .Where(x => x.PostedAt == null
                        && x.Reference != null
                        && x.Reference.StartsWith(ReferencePrefix))
            .ToListAsync(ct);

        if (pending.Count == 0)
        {
            return 0;
        }

        foreach (var entry in pending)
        {
            entry.PostedAt = postedAt;
        }

        await db.SaveChangesAsync(ct);
        return pending.Count;
    }

    public static async Task<int> TryAutoPostAtMonthEndAsync(ApplicationDbContext db, CancellationToken ct)
    {
        if (!IsLastDayOfMonth(DateTime.UtcNow.Date))
        {
            return 0;
        }

        return await PostPendingSalaryAccrualsAsync(db, DateTimeOffset.UtcNow, ct);
    }
}
