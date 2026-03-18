using AO.API.Authentication;
using AO.API.Helpers;
using AO.Core.Features.Deals;
using AO.Core.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AO.API.Controllers;

[Authorize(AuthenticationSchemes = ApiKeyAuthenticationHandler.SchemeName)]
[Route("api/[controller]")]
[ApiController]
public class DealsController(AOContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var deals = await DealSlice.GetAllAsync(dbContext, cancellationToken);
        return Ok(ResponseFactory.Success(deals, "Deals retrieved successfully.", StatusCodes.Status200OK));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var deal = await DealSlice.GetByIdAsync(dbContext, id, cancellationToken);
        if (deal is null)
        {
            return NotFound(ResponseFactory.Failure("Deal not found.", StatusCodes.Status404NotFound));
        }

        return Ok(ResponseFactory.Success(deal, "Deal retrieved successfully.", StatusCodes.Status200OK));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDealRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var deal = await DealSlice.CreateAsync(dbContext, request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = deal.Id }, ResponseFactory.Success(deal, "Deal created successfully.", StatusCodes.Status201Created));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(ResponseFactory.Failure(exception.Message, StatusCodes.Status400BadRequest));
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDealRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var deal = await DealSlice.UpdateAsync(dbContext, id, request, cancellationToken);
            if (deal is null)
            {
                return NotFound(ResponseFactory.Failure("Deal not found.", StatusCodes.Status404NotFound));
            }

            return Ok(ResponseFactory.Success(deal, "Deal updated successfully.", StatusCodes.Status200OK));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(ResponseFactory.Failure(exception.Message, StatusCodes.Status400BadRequest));
        }
    }

    [HttpPatch("{id:guid}/stage")]
    public async Task<IActionResult> SetStage(Guid id, [FromBody] SetDealStageRequest request, CancellationToken cancellationToken)
    {
        var deal = await DealSlice.SetStageAsync(dbContext, id, request, cancellationToken);
        if (deal is null)
        {
            return NotFound(ResponseFactory.Failure("Deal not found.", StatusCodes.Status404NotFound));
        }

        return Ok(ResponseFactory.Success(deal, "Deal stage updated successfully.", StatusCodes.Status200OK));
    }
}
