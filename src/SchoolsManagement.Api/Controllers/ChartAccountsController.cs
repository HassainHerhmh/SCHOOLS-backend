using System.Data;
using System.Data.Common;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolsManagement.Api.Data;
using SchoolsManagement.Api.Models.Accounting;

namespace SchoolsManagement.Api.Controllers;

[ApiController]
[Route("api/accountss")]
[AllowAnonymous]
public class ChartAccountsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public ChartAccountsController(ApplicationDbContext db)
    {
        _db = db;
    }

    /// <summary>يشخّص شكل جدول accountss: وجود pk_id، وهل هو IDENTITY، وهل id هو IDENTITY (كما في قواعد مستوردة من Supabase).</summary>
    private sealed record AccountssInsertMeta(bool HasPkIdColumn, bool PkIdIsIdentity, bool IdIsIdentity);

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AccountssRow>>> List(CancellationToken cancellationToken)
    {
        return Ok(await ReadAccountssAsync(null, cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AccountssRow>> GetById(int id, CancellationToken cancellationToken)
    {
        var row = (await ReadAccountssAsync(id, cancellationToken)).FirstOrDefault();
        return row is null ? NotFound() : Ok(row);
    }

    [HttpPost]
    public async Task<ActionResult<AccountssRow>> Create(
        [FromBody] UpsertChartAccountCreateRequest body,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(body.Code))
        {
            return BadRequest(new { message = "Account code is required." });
        }

        if (string.IsNullOrWhiteSpace(body.NameAr))
        {
            return BadRequest(new { message = "Arabic account name is required." });
        }

        if (string.IsNullOrWhiteSpace(body.AccountLevel))
        {
            return BadRequest(new { message = "Account level is required." });
        }

        AccountssInsertMeta meta;
        try
        {
            meta = await LoadAccountssInsertMetaAsync(cancellationToken);
        }
        catch (SqlException ex)
        {
            return StatusCode(500, new { message = ex.Message, step = "detect_schema" });
        }

        int logicalId;
        if (meta.IdIsIdentity)
        {
            logicalId = 0;
        }
        else
        {
            logicalId = body.Id ?? await GetNextAccountIdAsync(cancellationToken);
            if (await AccountExistsAsync(logicalId, cancellationToken))
            {
                return Conflict(new { message = "Account id already exists." });
            }
        }

        var sql = BuildAccountssInsertSql(meta);
        int createdId;

        try
        {
            createdId = await ExecuteInsertReturningIdAsync(
                sql,
                command =>
                {
                    if (!meta.IdIsIdentity)
                    {
                        AddParameter(command, "@id", logicalId);
                    }

                    AddParameter(command, "@code", body.Code.Trim());
                    AddParameter(command, "@name_ar", body.NameAr.Trim());
                    AddParameter(command, "@name_en", body.NameEn?.Trim() ?? string.Empty);
                    AddParameter(command, "@parent_id", body.ParentId);
                    AddParameter(command, "@account_group_id", body.AccountGroupId);
                    AddParameter(command, "@account_level", body.AccountLevel.Trim());
                    AddParameter(command, "@financial_statement_id", EmptyToNull(body.FinancialStatementId));
                    // datetime يتوافق مع أعمدة datetime / datetime2 في SQL Server
                    AddParameter(command, "@created_at", DateTime.UtcNow);
                    AddParameter(command, "@created_by", EmptyToNull(body.CreatedBy));
                    AddParameter(command, "@branch_id", EmptyToNull(body.BranchId));
                },
                cancellationToken);
        }
        catch (SqlException ex)
        {
            return StatusCode(500, new { message = ex.Message, step = "insert", number = ex.Number });
        }

        var created = (await ReadAccountssAsync(createdId, cancellationToken)).FirstOrDefault();
        return CreatedAtAction(nameof(GetById), new { id = createdId }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<AccountssRow>> Update(
        int id,
        [FromBody] JsonElement body,
        CancellationToken cancellationToken)
    {
        if (!await AccountExistsAsync(id, cancellationToken))
        {
            return NotFound();
        }

        var nameAr = GetPropertyString(body, "name_ar");
        if (body.TryGetProperty("name_ar", out _) && string.IsNullOrWhiteSpace(nameAr))
        {
            return BadRequest(new { message = "Arabic account name is required." });
        }

        try
        {
            await ExecuteNonQueryAsync(
                """
UPDATE dbo.accountss
SET
    code = COALESCE(@code, code),
    name_ar = COALESCE(@name_ar, name_ar),
    name_en = COALESCE(@name_en, name_en),
    parent_id = CASE WHEN @has_parent_id = 1 THEN @parent_id ELSE parent_id END,
    account_group_id = CASE WHEN @has_account_group_id = 1 THEN @account_group_id ELSE account_group_id END,
    account_level = COALESCE(@account_level, account_level),
    financial_statement_id = CASE WHEN @has_financial_statement_id = 1 THEN @financial_statement_id ELSE financial_statement_id END
WHERE TRY_CONVERT(int, id) = @where_id
""",
                command =>
                {
                    AddParameter(command, "@where_id", id);
                    AddParameter(command, "@code", GetPropertyString(body, "code"));
                    AddParameter(command, "@name_ar", nameAr);
                    AddParameter(command, "@name_en", GetPropertyString(body, "name_en"));
                    AddParameter(command, "@has_parent_id", body.TryGetProperty("parent_id", out _) ? 1 : 0);
                    AddParameter(command, "@parent_id", GetPropertyIntNullable(body, "parent_id"));
                    AddParameter(command, "@has_account_group_id", body.TryGetProperty("account_group_id", out _) ? 1 : 0);
                    AddParameter(command, "@account_group_id", GetPropertyIntNullable(body, "account_group_id"));
                    AddParameter(command, "@account_level", GetPropertyString(body, "account_level"));
                    AddParameter(command, "@has_financial_statement_id", body.TryGetProperty("financial_statement_id", out _) ? 1 : 0);
                    AddParameter(command, "@financial_statement_id", GetPropertyString(body, "financial_statement_id"));
                },
                cancellationToken);
        }
        catch (SqlException ex)
        {
            return StatusCode(500, new { message = ex.Message, step = "update", number = ex.Number });
        }

        return Ok((await ReadAccountssAsync(id, cancellationToken)).FirstOrDefault());
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        if (!await AccountExistsAsync(id, cancellationToken))
        {
            return NotFound();
        }

        if (await HasChildAccountsAsync(id, cancellationToken))
        {
            return BadRequest(new { message = "Cannot delete an account that has child accounts." });
        }

        await ExecuteNonQueryAsync(
            "DELETE FROM dbo.accountss WHERE TRY_CONVERT(int, id) = @id",
            command => AddParameter(command, "@id", id),
            cancellationToken);

        return NoContent();
    }

    private async Task<List<AccountssRow>> ReadAccountssAsync(int? id, CancellationToken cancellationToken)
    {
        var rows = new List<AccountssRow>();

        await WithCommandAsync(
            """
SELECT
    id,
    code,
    name_ar,
    name_en,
    parent_id,
    account_group_id,
    account_level,
    financial_statement_id,
    created_at,
    created_by,
    branch_id
FROM dbo.accountss
WHERE (@id IS NULL OR TRY_CONVERT(int, id) = @id)
ORDER BY TRY_CONVERT(int, id), id
""",
            command =>
            {
                AddParameter(command, "@id", id);
                return Task.CompletedTask;
            },
            async command =>
            {
                await using var reader = await command.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    rows.Add(new AccountssRow
                    {
                        id = ToInt(reader["id"]),
                        code = ToText(reader["code"]),
                        name_ar = ToText(reader["name_ar"]),
                        name_en = ToText(reader["name_en"]),
                        parent_id = ToNullableInt(reader["parent_id"]),
                        account_group_id = ToNullableInt(reader["account_group_id"]),
                        account_level = ToText(reader["account_level"]),
                        financial_statement_id = ToNullableText(reader["financial_statement_id"]),
                        created_at = ToNullableText(reader["created_at"]),
                        created_by = ToNullableText(reader["created_by"]),
                        branch_id = ToNullableText(reader["branch_id"])
                    });
                }
            },
            cancellationToken);

        return rows;
    }

    private async Task<int> GetNextAccountIdAsync(CancellationToken cancellationToken)
    {
        var result = await ExecuteScalarAsync(
            "SELECT ISNULL(MAX(TRY_CONVERT(int, id)), 0) + 1 FROM dbo.accountss",
            _ => { },
            cancellationToken);

        return ToInt(result);
    }

    private async Task<bool> AccountExistsAsync(int id, CancellationToken cancellationToken)
    {
        var result = await ExecuteScalarAsync(
            "SELECT COUNT(1) FROM dbo.accountss WHERE TRY_CONVERT(int, id) = @id",
            command => AddParameter(command, "@id", id),
            cancellationToken);

        return ToInt(result) > 0;
    }

    private async Task<bool> HasChildAccountsAsync(int id, CancellationToken cancellationToken)
    {
        var result = await ExecuteScalarAsync(
            "SELECT COUNT(1) FROM dbo.accountss WHERE TRY_CONVERT(int, parent_id) = @id",
            command => AddParameter(command, "@id", id),
            cancellationToken);

        return ToInt(result) > 0;
    }

    /// <summary>
    /// لا نستخدم COL_LENGTH لوجود عمود pk_id — قد يُرجع NULL خطأ؛ نعتمد sys.columns و is_identity.
    /// </summary>
    private async Task<AccountssInsertMeta> LoadAccountssInsertMetaAsync(CancellationToken cancellationToken)
    {
        var connection = _db.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;

        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = """
SELECT
    CASE WHEN EXISTS (
        SELECT 1 FROM sys.columns
        WHERE object_id = OBJECT_ID(N'dbo.accountss') AND name = N'pk_id'
    ) THEN 1 ELSE 0 END,
    CASE WHEN EXISTS (
        SELECT 1 FROM sys.columns
        WHERE object_id = OBJECT_ID(N'dbo.accountss') AND name = N'pk_id' AND is_identity = 1
    ) THEN 1 ELSE 0 END,
    CASE WHEN EXISTS (
        SELECT 1 FROM sys.columns
        WHERE object_id = OBJECT_ID(N'dbo.accountss') AND name = N'id' AND is_identity = 1
    ) THEN 1 ELSE 0 END
""";

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                throw new InvalidOperationException("Could not read accountss column metadata.");
            }

            var hasPk = ToInt(reader[0]) != 0;
            var pkIsIdentity = ToInt(reader[1]) != 0;
            var idIsIdentity = ToInt(reader[2]) != 0;
            return new AccountssInsertMeta(hasPk, pkIsIdentity, idIsIdentity);
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static string BuildAccountssInsertSql(AccountssInsertMeta meta)
    {
        var cols = new List<string>();
        var vals = new List<string>();

        if (meta.HasPkIdColumn && !meta.PkIdIsIdentity)
        {
            cols.Add("pk_id");
            vals.Add("(SELECT ISNULL(MAX(pk_id), 0) + 1 FROM dbo.accountss)");
        }

        if (!meta.IdIsIdentity)
        {
            cols.Add("id");
            vals.Add("@id");
        }

        string[] tailCols =
        [
            "code",
            "name_ar",
            "name_en",
            "parent_id",
            "account_group_id",
            "account_level",
            "financial_statement_id",
            "created_at",
            "created_by",
            "branch_id"
        ];
        string[] tailParams =
        [
            "@code",
            "@name_ar",
            "@name_en",
            "@parent_id",
            "@account_group_id",
            "@account_level",
            "@financial_statement_id",
            "@created_at",
            "@created_by",
            "@branch_id"
        ];
        cols.AddRange(tailCols);
        vals.AddRange(tailParams);

        return $"""
INSERT INTO dbo.accountss ({string.Join(", ", cols)})
OUTPUT INSERTED.id
VALUES ({string.Join(", ", vals)})
""";
    }

    private async Task<int> ExecuteInsertReturningIdAsync(
        string sql,
        Action<DbCommand> configure,
        CancellationToken cancellationToken)
    {
        var connection = _db.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;

        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            configure(command);
            var scalar = await command.ExecuteScalarAsync(cancellationToken);
            return ToInt(scalar);
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }

    private async Task ExecuteNonQueryAsync(
        string sql,
        Action<DbCommand> configure,
        CancellationToken cancellationToken)
    {
        await WithCommandAsync(
            sql,
            command =>
            {
                configure(command);
                return Task.CompletedTask;
            },
            async command => await command.ExecuteNonQueryAsync(cancellationToken),
            cancellationToken);
    }

    private async Task<object?> ExecuteScalarAsync(
        string sql,
        Action<DbCommand> configure,
        CancellationToken cancellationToken)
    {
        object? result = null;

        await WithCommandAsync(
            sql,
            command =>
            {
                configure(command);
                return Task.CompletedTask;
            },
            async command => result = await command.ExecuteScalarAsync(cancellationToken),
            cancellationToken);

        return result;
    }

    private async Task WithCommandAsync(
        string sql,
        Func<DbCommand, Task> configure,
        Func<DbCommand, Task> execute,
        CancellationToken cancellationToken)
    {
        var connection = _db.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;

        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            await configure(command);
            await execute(command);
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static void AddParameter(DbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }

    private static string? GetPropertyString(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.String => EmptyToNull(value.GetString()),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => EmptyToNull(value.GetRawText())
        };
    }

    private static int? GetPropertyIntNullable(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var n))
        {
            return n;
        }

        if (value.ValueKind == JsonValueKind.String)
        {
            var s = value.GetString();
            if (string.IsNullOrWhiteSpace(s))
            {
                return null;
            }

            return int.TryParse(s.Trim(), out var parsed) ? parsed : null;
        }

        return null;
    }

    private static string? EmptyToNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static int ToInt(object? value)
    {
        if (value == DBNull.Value || value is null)
        {
            return 0;
        }

        if (value is int number)
        {
            return number;
        }

        return int.TryParse(Convert.ToString(value), out var parsed) ? parsed : 0;
    }

    private static int? ToNullableInt(object? value)
    {
        if (value == DBNull.Value || value is null)
        {
            return null;
        }

        if (value is int number)
        {
            return number;
        }

        return int.TryParse(Convert.ToString(value), out var parsed) ? parsed : null;
    }

    private static string ToText(object? value)
    {
        return value == DBNull.Value || value is null ? string.Empty : Convert.ToString(value) ?? string.Empty;
    }

    private static string? ToNullableText(object? value)
    {
        return value == DBNull.Value || value is null ? null : Convert.ToString(value);
    }

    public sealed class AccountssRow
    {
        public int id { get; set; }
        public string code { get; set; } = string.Empty;
        public string name_ar { get; set; } = string.Empty;
        public string name_en { get; set; } = string.Empty;
        public int? parent_id { get; set; }
        public int? account_group_id { get; set; }
        public string account_level { get; set; } = string.Empty;
        public string? financial_statement_id { get; set; }
        public string? created_at { get; set; }
        public string? created_by { get; set; }
        public string? branch_id { get; set; }
    }
}
