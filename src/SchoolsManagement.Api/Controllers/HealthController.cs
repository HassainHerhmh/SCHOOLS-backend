using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolsManagement.Api.Configuration;
using SchoolsManagement.Api.Data;
using SchoolsManagement.Api.Services;

namespace SchoolsManagement.Api.Controllers;

[ApiController]
[Route("api/health")]
[AllowAnonymous]
public class HealthController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly DatabaseHealthChecker _checker;
    private readonly DatabaseConfigState _dbConfig;

    public HealthController(
        ApplicationDbContext db,
        DatabaseHealthChecker checker,
        DatabaseConfigState dbConfig)
    {
        _db = db;
        _checker = checker;
        _dbConfig = dbConfig;
    }

    [HttpGet]
    public IActionResult Ping() => Ok(new
    {
        status = "ok",
        service = "SchoolsManagement.Api",
        database_configured = _dbConfig.IsConfigured,
        check_database = "/api/health/db",
        setup_help = "/api/health/setup"
    });

    [HttpGet("setup")]
    public IActionResult Setup() => Ok(new
    {
        status = _dbConfig.IsConfigured ? "configured" : "missing",
        message = _dbConfig.IsConfigured
            ? "Connection string موجود."
            : ConnectionStringResolver.BuildMissingConnectionMessage(),
        connection_summary = ConnectionStringResolver.RedactForDisplay(_dbConfig.ConnectionString)
    });

    [HttpGet("db")]
    public async Task<IActionResult> Database(CancellationToken cancellationToken)
    {
        if (!_dbConfig.IsConfigured)
        {
            return StatusCode(503, new
            {
                status = "error",
                message = ConnectionStringResolver.BuildMissingConnectionMessage(),
                setup_help = "/api/health/setup"
            });
        }

        var report = await _checker.CheckAsync(_db, _dbConfig.ConnectionString, cancellationToken);
        var statusCode = report.Status switch
        {
            "error" => StatusCodes.Status503ServiceUnavailable,
            "warning" => StatusCodes.Status200OK,
            _ => StatusCodes.Status200OK
        };
        return StatusCode(statusCode, report);
    }
}
