using CreditScanAI.Api.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CreditScanAI.Api.Controllers;

[ApiController]
[Route("api/health")]
[AllowAnonymous]
public class HealthController : ControllerBase
{
    [HttpGet]
    public ActionResult<ApiResponse<object>> Get()
    {
        var data = new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow
        };

        return Ok(ApiResponse<object>.Ok(data));
    }
}
