using AO.API.Authentication;
using AO.API.Helpers;
using AO.Core.Features.Reports;
using AO.Core.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AO.API.Controllers;

[Authorize(AuthenticationSchemes = ApiKeyAuthenticationHandler.SchemeName)]
[Route("api/[controller]")]
[ApiController]
public sealed class ReportsController(AOContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] GetReportsRequest request, CancellationToken cancellationToken)
    {
        var report = await ReportSlice.GetAsync(dbContext, request, cancellationToken);
        return Ok(ResponseFactory.Success(report, "Report retrieved successfully.", StatusCodes.Status200OK));
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] GetReportsRequest request, CancellationToken cancellationToken)
    {
        var export = await ReportSlice.ExportAsync(dbContext, request, cancellationToken);
        return Ok(ResponseFactory.Success(export, "Report export generated successfully.", StatusCodes.Status200OK));
    }
}
