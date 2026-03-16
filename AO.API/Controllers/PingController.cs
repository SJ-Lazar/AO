using Microsoft.AspNetCore.Mvc;
using AO.API.Helpers;

namespace AO.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PingController : ControllerBase
{
    private readonly ILogger<PingController> _logger;
    private readonly IConfiguration _configuration;

    public PingController(ILogger<PingController> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }


    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var validationState = ApiKeyHelper.ValidateRequest(_configuration, Request);

        if (validationState.ValidationResult == ApiKeyValidationResult.MissingConfiguration)
        {
            _logger.LogWarning(validationState.Message);
            return Problem(statusCode: StatusCodes.Status500InternalServerError, title: validationState.Message);
        }

        if (!validationState.IsValid)
        {
            _logger.LogWarning("Ping request rejected due to API key validation failure: {ValidationResult}. {Message}", validationState.ValidationResult, validationState.Message);
            return Unauthorized();
        }

        HttpContext.Request.EnableBuffering();

        using var reader = new StreamReader(
            HttpContext.Request.Body,
            System.Text.Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            leaveOpen: true);

        var bodyText = await reader.ReadToEndAsync();
        HttpContext.Request.Body.Position = 0;

        _logger.LogInformation("Ping requested");

        return Ok(new
        {
            ClientIpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UtcTime = DateTime.UtcNow,
            RemotePort = HttpContext.Connection.RemotePort,
            Body = bodyText
        });
    }

}
