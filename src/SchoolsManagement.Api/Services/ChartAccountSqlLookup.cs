using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using SchoolsManagement.Api.Data;

namespace SchoolsManagement.Api.Services;

/// <summary>
/// قراءة دليل الحسابات عبر SQL مع TRY_CONVERT لأن جداول accountss المستوردة قد تخزّن id/parent_id كنص.
/// </summary>
public sealed record ChartAccountLookupRow(int Id, int? ParentId, string Code, string NameAr);

public static class ChartAccountSqlLookup
{
    public static async Task<ChartAccountLookupRow?> GetByIdAsync(ApplicationDbContext db, int id, CancellationToken ct)
    {
        ChartAccountLookupRow? found = null;
        await RunWithConnectionAsync(
            db,
            async conn =>
            {
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = """
SELECT TOP (1)
    TRY_CONVERT(int, id),
    TRY_CONVERT(int, parent_id),
    COALESCE(code, N''),
    COALESCE(name_ar, N'')
FROM dbo.accountss
WHERE TRY_CONVERT(int, id) = @id
""";
                AddParam(cmd, "@id", id);
                await using var reader = await cmd.ExecuteReaderAsync(ct);
                if (await reader.ReadAsync(ct))
                {
                    found = ReadRow(reader);
                }
            },
            ct);
        return found;
    }

    public static async Task<Dictionary<int, ChartAccountLookupRow>> GetByIdsAsync(
        ApplicationDbContext db,
        IReadOnlyList<int> ids,
        CancellationToken ct)
    {
        var dict = new Dictionary<int, ChartAccountLookupRow>();
        var distinct = ids.Where(static i => i > 0).Distinct().ToList();
        if (distinct.Count == 0)
        {
            return dict;
        }

        await RunWithConnectionAsync(
            db,
            async conn =>
            {
                const int chunkSize = 400;
                for (var offset = 0; offset < distinct.Count; offset += chunkSize)
                {
                    var chunk = distinct.Skip(offset).Take(chunkSize).ToList();
                    var names = string.Join(",", chunk.Select((_, i) => $"@p{i}"));
                    await using var cmd = conn.CreateCommand();
                    cmd.CommandText = $"""
SELECT
    TRY_CONVERT(int, id),
    TRY_CONVERT(int, parent_id),
    COALESCE(code, N''),
    COALESCE(name_ar, N'')
FROM dbo.accountss
WHERE TRY_CONVERT(int, id) IN ({names})
""";
                    for (var i = 0; i < chunk.Count; i++)
                    {
                        AddParam(cmd, $"@p{i}", chunk[i]);
                    }

                    await using var reader = await cmd.ExecuteReaderAsync(ct);
                    while (await reader.ReadAsync(ct))
                    {
                        var row = ReadRow(reader);
                        dict[row.Id] = row;
                    }
                }
            },
            ct);

        return dict;
    }

    public static async Task<bool> HasChildAccountsAsync(ApplicationDbContext db, int parentId, CancellationToken ct)
    {
        var count = 0;
        await RunWithConnectionAsync(
            db,
            async conn =>
            {
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = """
SELECT COUNT(1) FROM dbo.accountss WHERE TRY_CONVERT(int, parent_id) = @pid
""";
                AddParam(cmd, "@pid", parentId);
                var scalar = await cmd.ExecuteScalarAsync(ct);
                count = ToInt(scalar);
            },
            ct);
        return count > 0;
    }

    private static ChartAccountLookupRow ReadRow(DbDataReader reader)
    {
        var id = reader.GetInt32(0);
        var pid = reader.IsDBNull(1) ? (int?)null : reader.GetInt32(1);
        var code = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);
        var nameAr = reader.IsDBNull(3) ? string.Empty : reader.GetString(3);
        return new ChartAccountLookupRow(id, pid, code, nameAr);
    }

    private static async Task RunWithConnectionAsync(
        ApplicationDbContext db,
        Func<DbConnection, Task> action,
        CancellationToken ct)
    {
        var conn = db.Database.GetDbConnection();
        var shouldClose = conn.State != ConnectionState.Open;
        if (shouldClose)
        {
            await conn.OpenAsync(ct);
        }

        try
        {
            await action(conn);
        }
        finally
        {
            if (shouldClose)
            {
                await conn.CloseAsync();
            }
        }
    }

    private static void AddParam(DbCommand cmd, string name, object value)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.Value = value ?? DBNull.Value;
        cmd.Parameters.Add(p);
    }

    private static int ToInt(object? value)
    {
        if (value == null || value == DBNull.Value)
        {
            return 0;
        }

        return Convert.ToInt32(value);
    }
}
