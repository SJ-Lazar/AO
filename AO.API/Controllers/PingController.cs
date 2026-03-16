using Microsoft.AspNetCore.Mvc;
using AO.API.Authentication;
using AO.Core.Shared.ApiResponses;
using Microsoft.AspNetCore.Authorization;

namespace AO.API.Controllers;

[Authorize(AuthenticationSchemes = ApiKeyAuthenticationHandler.SchemeName)]
[Route("api/[controller]")]
[ApiController]
public class PingController : ControllerBase
{
    private readonly ILogger<PingController> _logger;

    public PingController(ILogger<PingController> logger)
    {
        _logger = logger;
    }


    [HttpGet]
    public async Task<IActionResult> Get()
    {
        HttpContext.Request.EnableBuffering();

        using var reader = new StreamReader(
            HttpContext.Request.Body,
            System.Text.Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            leaveOpen: true);

        var bodyText = await reader.ReadToEndAsync();
        HttpContext.Request.Body.Position = 0;

        _logger.LogInformation("Ping requested");

        return Ok(new Response<object>
        {
            Sucesss = true,
            Message = "Ping successful.",
            StatusCode = StatusCodes.Status200OK,
            Data = new
            {
                ClientIpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UtcTime = DateTime.UtcNow,
                RemotePort = HttpContext.Connection.RemotePort,
                Body = bodyText
            }
        });
    }

}
