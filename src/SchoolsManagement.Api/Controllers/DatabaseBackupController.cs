using System.Data;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolsManagement.Api.Data;
using SchoolsManagement.Api.Models.Backup;

namespace SchoolsManagement.Api.Controllers;

[ApiController]
[Route("api/database-backup")]
[Authorize]
public class DatabaseBackupController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public DatabaseBackupController(ApplicationDbContext db) => _db = db;

    [HttpGet("tables")]
    public async Task<ActionResult<IEnumerable<BackupTableInfo>>> Tables(CancellationToken ct)
    {
        var discovered = await DiscoverDboTablesAsync(ct);
        var orderedKeys = DatabaseBackupCatalog.SortTableKeys(discovered);
        var result = new List<BackupTableInfo>();

        foreach (var key in orderedKeys)
        {
            var columns = await DiscoverColumnsAsync(DatabaseBackupCatalog.ResolveSqlTableName(key), ct);
            result.Add(new BackupTableInfo
            {
                Key = key,
                LabelAr = DatabaseBackupCatalog.GetTableLabel(key),
                Columns = columns
                    .Select(c => new BackupColumnInfo
                    {
                        Key = c,
                        LabelAr = DatabaseBackupCatalog.GetColumnLabel(key, c)
                    })
                    .ToList()
            });
        }

        return Ok(result);
    }

    [HttpGet("export/{tableKey}")]
    public async Task<ActionResult<IEnumerable<JsonElement>>> Export(string tableKey, CancellationToken ct)
    {
        if (!await TableExistsAsync(tableKey, ct))
        {
            return NotFound(new { message = "جدول غير معروف أو غير موجود في القاعدة." });
        }

        var sqlName = DatabaseBackupCatalog.ResolveSqlTableName(tableKey);
        var rows = await ReadTableRowsAsync(sqlName, ct);
        var json = JsonSerializer.SerializeToElement(rows);
        if (json.ValueKind == JsonValueKind.Array)
        {
            return Ok(json.EnumerateArray().ToList());
        }

        return Ok(Array.Empty<JsonElement>());
    }

    private async Task<HashSet<string>> DiscoverDboTablesAsync(CancellationToken ct)
    {
        var conn = _db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
        {
            await conn.OpenAsync(ct);
        }

        var tables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
            """
            SELECT TABLE_NAME
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_SCHEMA = N'dbo' AND TABLE_TYPE = N'BASE TABLE'
            ORDER BY TABLE_NAME
            """;

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var name = reader.GetString(0);
            if (!DatabaseBackupCatalog.IsExcludedTable(name))
            {
                tables.Add(name);
            }
        }

        return tables;
    }

    private async Task<bool> TableExistsAsync(string tableKey, CancellationToken ct)
    {
        var discovered = await DiscoverDboTablesAsync(ct);
        var sqlName = DatabaseBackupCatalog.ResolveSqlTableName(tableKey);
        return discovered.Contains(tableKey) || discovered.Contains(sqlName);
    }

    private async Task<IReadOnlyList<string>> DiscoverColumnsAsync(string sqlTableName, CancellationToken ct)
    {
        var conn = _db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
        {
            await conn.OpenAsync(ct);
        }

        var columns = new List<string>();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
            """
            SELECT COLUMN_NAME
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = N'dbo' AND TABLE_NAME = @table
            ORDER BY ORDINAL_POSITION
            """;
        var param = cmd.CreateParameter();
        param.ParameterName = "@table";
        param.Value = sqlTableName;
        cmd.Parameters.Add(param);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            columns.Add(reader.GetString(0));
        }

        return columns;
    }

    private async Task<List<Dictionary<string, object?>>> ReadTableRowsAsync(string sqlName, CancellationToken ct)
    {
        var conn = _db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
        {
            await conn.OpenAsync(ct);
        }

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT * FROM dbo.[{sqlName.Replace("]", "]]")}]";
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var rows = new List<Dictionary<string, object?>>();
        while (await reader.ReadAsync(ct))
        {
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < reader.FieldCount; i++)
            {
                var val = reader.GetValue(i);
                row[reader.GetName(i)] = val == DBNull.Value ? null : val;
            }

            rows.Add(row);
        }

        return rows;
    }
}
