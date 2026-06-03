using SchoolsManagement.Api.Models.Accounting;

namespace SchoolsManagement.Api.Services;

public static class AccountingVoucherAuditHelper
{
    public static async Task ApplyOnCreateAsync(
        IAccountingCreatedByAudit target,
        IAccountingCreatedByAudit? body,
        AccountingCurrentUserService currentUser,
        CancellationToken ct)
    {
        var userId = await currentUser.ResolveUserIdAsync(ct);
        var displayName = await currentUser.ResolveDisplayNameAsync(ct);

        target.CreatedByUserId = FirstNonEmpty(body?.CreatedByUserId, userId);
        target.CreatedByName = FirstNonEmpty(body?.CreatedByName?.Trim(), displayName);
        if (target.CreatedBy is null or 0)
        {
            target.CreatedBy = body?.CreatedBy;
        }
    }

    public static async Task ApplyOnUpdateAsync(
        IAccountingCreatedByAudit target,
        IAccountingCreatedByAudit? body,
        AccountingCurrentUserService currentUser,
        CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(body?.CreatedByName))
        {
            target.CreatedByName = body.CreatedByName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(body?.CreatedByUserId))
        {
            target.CreatedByUserId = body.CreatedByUserId.Trim();
        }

        if (string.IsNullOrWhiteSpace(target.CreatedByName))
        {
            target.CreatedByName = await currentUser.ResolveDisplayNameAsync(ct);
        }

        if (string.IsNullOrWhiteSpace(target.CreatedByUserId))
        {
            target.CreatedByUserId = await currentUser.ResolveUserIdAsync(ct);
        }
    }

    public static async Task EnrichDisplayNamesAsync<T>(
        IEnumerable<T> rows,
        VoucherUserNameEnricher enricher,
        CancellationToken ct)
        where T : IAccountingCreatedByAudit
    {
        foreach (var row in rows)
        {
            row.CreatedByName = await enricher.ResolveMissingNameAsync(
                row.CreatedByUserId,
                row.CreatedByName,
                ct);
        }
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }
}
