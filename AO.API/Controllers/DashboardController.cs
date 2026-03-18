using AO.API.Authentication;
using AO.API.Helpers;
using AO.Core.Features.Dashboard;
using AO.Core.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AO.API.Controllers;

[Authorize(AuthenticationSchemes = ApiKeyAuthenticationHandler.SchemeName)]
[Route("api/[controller]")]
[ApiController]
public class DashboardController(AOContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var dashboard = await DashboardSlice.GetAsync(dbContext, cancellationToken);
        return Ok(ResponseFactory.Success(dashboard, "Dashboard retrieved successfully.", StatusCodes.Status200OK));
    }
}
