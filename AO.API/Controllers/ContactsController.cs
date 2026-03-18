using AO.API.Authentication;
using AO.API.Helpers;
using AO.Core.Features.Contacts;
using AO.Core.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AO.API.Controllers;

[Authorize(AuthenticationSchemes = ApiKeyAuthenticationHandler.SchemeName)]
[Route("api/[controller]")]
[ApiController]
public class ContactsController(AOContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var contacts = await ContactSlice.GetAllAsync(dbContext, cancellationToken);
        return Ok(ResponseFactory.Success(contacts, "Contacts retrieved successfully.", StatusCodes.Status200OK));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var contact = await ContactSlice.GetByIdAsync(dbContext, id, cancellationToken);
        if (contact is null)
        {
            return NotFound(ResponseFactory.Failure("Contact not found.", StatusCodes.Status404NotFound));
        }

        return Ok(ResponseFactory.Success(contact, "Contact retrieved successfully.", StatusCodes.Status200OK));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateContactRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var contact = await ContactSlice.CreateAsync(dbContext, request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = contact.Id }, ResponseFactory.Success(contact, "Contact created successfully.", StatusCodes.Status201Created));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(ResponseFactory.Failure(exception.Message, StatusCodes.Status400BadRequest));
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateContactRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var contact = await ContactSlice.UpdateAsync(dbContext, id, request, cancellationToken);
            if (contact is null)
            {
                return NotFound(ResponseFactory.Failure("Contact not found.", StatusCodes.Status404NotFound));
            }

            return Ok(ResponseFactory.Success(contact, "Contact updated successfully.", StatusCodes.Status200OK));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(ResponseFactory.Failure(exception.Message, StatusCodes.Status400BadRequest));
        }
    }
}
