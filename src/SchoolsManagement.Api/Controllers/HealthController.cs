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
    private readonly string _connectionString;

    public HealthController(ApplicationDbContext db, DatabaseHealthChecker checker, IConfiguration config)
    {
        _db = db;
        _checker = checker;
        _connectionString = ConnectionStringResolver.Resolve(config);
    }

    [HttpGet]
    public IActionResult Ping() => Ok(new
    {
        status = "ok",
        service = "SchoolsManagement.Api",
        sql_configured = !ConnectionStringResolver.LooksLikeLocalSql(_connectionString),
        check_database = "/api/health/db"
    });

    [HttpGet("db")]
    public async Task<IActionResult> Database(CancellationToken cancellationToken)
    {
        var report = await _checker.CheckAsync(_db, _connectionString, cancellationToken);
        var statusCode = report.Status switch
        {
            "error" => StatusCodes.Status503ServiceUnavailable,
            "warning" => StatusCodes.Status200OK,
            _ => StatusCodes.Status200OK
        };
        return StatusCode(statusCode, report);
    }
}
