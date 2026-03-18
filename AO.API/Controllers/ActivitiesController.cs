using AO.API.Authentication;
using AO.API.Helpers;
using AO.Core.Features.Activities;
using AO.Core.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AO.API.Controllers;

[Authorize(AuthenticationSchemes = ApiKeyAuthenticationHandler.SchemeName)]
[Route("api/[controller]")]
[ApiController]
public class ActivitiesController(AOContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetRecent([FromQuery] int take = 10, CancellationToken cancellationToken = default)
    {
        var activities = await ActivitySlice.GetRecentAsync(dbContext, take, cancellationToken);
        return Ok(ResponseFactory.Success(activities, "Activities retrieved successfully.", StatusCodes.Status200OK));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateActivityRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var activity = await ActivitySlice.CreateAsync(dbContext, request, cancellationToken);
            return Ok(ResponseFactory.Success(activity, "Activity created successfully.", StatusCodes.Status200OK));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(ResponseFactory.Failure(exception.Message, StatusCodes.Status400BadRequest));
        }
    }
}
