using AO.API.Authentication;
using AO.API.Helpers;
using AO.Core.Features.Users;
using AO.Core.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AO.API.Controllers;

[Authorize(AuthenticationSchemes = ApiKeyAuthenticationHandler.SchemeName)]
[Route("api/[controller]")]
[ApiController]
public sealed class UsersController(AOContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var users = await CrmUserSlice.GetAllAsync(dbContext, cancellationToken);
        return Ok(ResponseFactory.Success(users, "Users retrieved successfully.", StatusCodes.Status200OK));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var user = await CrmUserSlice.GetByIdAsync(dbContext, id, cancellationToken);
        if (user is null)
        {
            return NotFound(ResponseFactory.Failure("User not found.", StatusCodes.Status404NotFound));
        }

        return Ok(ResponseFactory.Success(user, "User retrieved successfully.", StatusCodes.Status200OK));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCrmUserRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await CrmUserSlice.CreateAsync(dbContext, request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = user.Id }, ResponseFactory.Success(user, "User created successfully.", StatusCodes.Status201Created));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(ResponseFactory.Failure(exception.Message, StatusCodes.Status400BadRequest));
        }
    }
}
