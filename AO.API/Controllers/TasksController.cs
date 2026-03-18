using AO.API.Authentication;
using AO.API.Helpers;
using AO.Core.Features.Tasks;
using AO.Core.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AO.API.Controllers;

[Authorize(AuthenticationSchemes = ApiKeyAuthenticationHandler.SchemeName)]
[Route("api/[controller]")]
[ApiController]
public class TasksController(AOContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var tasks = await TaskSlice.GetAllAsync(dbContext, cancellationToken);
        return Ok(ResponseFactory.Success(tasks, "Tasks retrieved successfully.", StatusCodes.Status200OK));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var task = await TaskSlice.GetByIdAsync(dbContext, id, cancellationToken);
        if (task is null)
        {
            return NotFound(ResponseFactory.Failure("Task not found.", StatusCodes.Status404NotFound));
        }

        return Ok(ResponseFactory.Success(task, "Task retrieved successfully.", StatusCodes.Status200OK));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCrmTaskRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var task = await TaskSlice.CreateAsync(dbContext, request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = task.Id }, ResponseFactory.Success(task, "Task created successfully.", StatusCodes.Status201Created));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(ResponseFactory.Failure(exception.Message, StatusCodes.Status400BadRequest));
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCrmTaskRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var task = await TaskSlice.UpdateAsync(dbContext, id, request, cancellationToken);
            if (task is null)
            {
                return NotFound(ResponseFactory.Failure("Task not found.", StatusCodes.Status404NotFound));
            }

            return Ok(ResponseFactory.Success(task, "Task updated successfully.", StatusCodes.Status200OK));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(ResponseFactory.Failure(exception.Message, StatusCodes.Status400BadRequest));
        }
    }

    [HttpPatch("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, CancellationToken cancellationToken)
    {
        var task = await TaskSlice.CompleteAsync(dbContext, id, cancellationToken);
        if (task is null)
        {
            return NotFound(ResponseFactory.Failure("Task not found.", StatusCodes.Status404NotFound));
        }

        return Ok(ResponseFactory.Success(task, "Task completed successfully.", StatusCodes.Status200OK));
    }
}
