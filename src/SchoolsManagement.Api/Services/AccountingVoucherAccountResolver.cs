using Microsoft.EntityFrameworkCore;
using SchoolsManagement.Api.Data;
using SchoolsManagement.Api.Models.Accounting;
namespace SchoolsManagement.Api.Services;

/// <summary>ربط صندوق/بنك بحساب فرعي في دليل الحسابات لسندات القبض والصرف.</summary>
public static class AccountingVoucherAccountResolver
{
    public static async Task<int?> ResolveCashBoxChartAccountIdAsync(
        ApplicationDbContext db,
        int cashBoxId,
        CancellationToken ct)
    {
        var box = await db.CashBoxes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == cashBoxId, ct);
        if (box is null)
        {
            return null;
        }

        if (box.AccountId is > 0)
        {
            return box.AccountId;
        }

        if (box.ParentAccountId is > 0)
        {
            return box.ParentAccountId;
        }

        return await FindChartAccountByCodeOrNameAsync(db, box.Code, box.NameAr, ct);
    }

    public static async Task<int?> ResolveBankChartAccountIdAsync(
        ApplicationDbContext db,
        int bankId,
        CancellationToken ct)
    {
        var bank = await db.Banks.AsNoTracking().FirstOrDefaultAsync(x => x.Id == bankId, ct);
        if (bank is null)
        {
            return null;
        }

        if (bank.AccountId is > 0)
        {
            return bank.AccountId;
        }

        if (bank.ParentAccountId is > 0)
        {
            return bank.ParentAccountId;
        }

        return await FindChartAccountByCodeOrNameAsync(db, bank.Code, bank.NameAr, ct);
    }

    private static async Task<int?> FindChartAccountByCodeOrNameAsync(
        ApplicationDbContext db,
        string code,
        string nameAr,
        CancellationToken ct)
    {
        var trimmedCode = (code ?? "").Trim();
        if (!string.IsNullOrEmpty(trimmedCode))
        {
            var accounts = await db.ChartAccounts.AsNoTracking()
                .Select(a => new { a.Id, a.Code })
                .ToListAsync(ct);
            var byCode = accounts.FirstOrDefault(a =>
                string.Equals((a.Code ?? "").Trim(), trimmedCode, StringComparison.OrdinalIgnoreCase));
            if (byCode is not null)
            {
                return byCode.Id;
            }
        }

        var name = (nameAr ?? "").Trim();
        if (string.IsNullOrEmpty(name))
        {
            return null;
        }

        return await db.ChartAccounts.AsNoTracking()
            .Where(a => a.NameAr == name)
            .Select(a => (int?)a.Id)
            .FirstOrDefaultAsync(ct);
    }
}
