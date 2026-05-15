using Microsoft.AspNetCore.Mvc;

namespace Schools.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() =>
        Ok(new
        {
            status = "ok",
            message = "الخادم يعمل — ابدأ بإضافة Controllers وربط SQL Server خطوة بخطوة."
        });
}
