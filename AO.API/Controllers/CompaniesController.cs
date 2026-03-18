using AO.API.Authentication;
using AO.API.Helpers;
using AO.Core.Features.Companies;
using AO.Core.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AO.API.Controllers;

[Authorize(AuthenticationSchemes = ApiKeyAuthenticationHandler.SchemeName)]
[Route("api/[controller]")]
[ApiController]
public class CompaniesController(AOContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var companies = await CompanySlice.GetAllAsync(dbContext, cancellationToken);
        return Ok(ResponseFactory.Success(companies, "Companies retrieved successfully.", StatusCodes.Status200OK));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var company = await CompanySlice.GetByIdAsync(dbContext, id, cancellationToken);
        if (company is null)
        {
            return NotFound(ResponseFactory.Failure("Company not found.", StatusCodes.Status404NotFound));
        }

        return Ok(ResponseFactory.Success(company, "Company retrieved successfully.", StatusCodes.Status200OK));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCompanyRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var company = await CompanySlice.CreateAsync(dbContext, request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = company.Id }, ResponseFactory.Success(company, "Company created successfully.", StatusCodes.Status201Created));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(ResponseFactory.Failure(exception.Message, StatusCodes.Status400BadRequest));
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCompanyRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var company = await CompanySlice.UpdateAsync(dbContext, id, request, cancellationToken);
            if (company is null)
            {
                return NotFound(ResponseFactory.Failure("Company not found.", StatusCodes.Status404NotFound));
            }

            return Ok(ResponseFactory.Success(company, "Company updated successfully.", StatusCodes.Status200OK));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(ResponseFactory.Failure(exception.Message, StatusCodes.Status400BadRequest));
        }
    }
}
