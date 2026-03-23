using AuthPlaypen.Application.Dtos;
using AuthPlaypen.Application.Services;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Mvc;

namespace AuthPlaypen.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ApplicationsController(IApplicationService applicationService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<CursorPagedResultDto<ApplicationDto>>> GetPage(
        [FromQuery] string? cursor,
        [FromQuery] int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (pageSize < 1 || pageSize > 100)
        {
            return BadRequest(new ProblemDetails { Title = "Invalid page size", Detail = "PageSize must be between 1 and 100." });
        }

        if (!string.IsNullOrWhiteSpace(cursor) && !Guid.TryParse(cursor, out _))
        {
            return BadRequest(new ProblemDetails { Title = "Invalid cursor", Detail = "Cursor must be a valid GUID." });
        }

        var parsedCursor = string.IsNullOrWhiteSpace(cursor) ? (Guid?)null : Guid.Parse(cursor);
        var result = await applicationService.GetPageAsync(parsedCursor, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("search")]
    public async Task<ActionResult<IReadOnlyCollection<ApplicationReferenceDto>>> Search(
        [FromQuery] string term,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            return Ok(Array.Empty<ApplicationReferenceDto>());
        }

        if (pageSize < 1 || pageSize > 100)
        {
            return BadRequest(new ProblemDetails { Title = "Invalid page size", Detail = "PageSize must be between 1 and 100." });
        }

        var results = await applicationService.SearchAsync(term, pageSize, cancellationToken);
        return Ok(results);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApplicationDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var application = await applicationService.GetByIdAsync(id, cancellationToken);
        return application is null ? NotFound() : Ok(application);
    }

    [HttpPost]
    public async Task<ActionResult<ApplicationDto>> Create(CreateApplicationRequest request, CancellationToken cancellationToken)
    {
        var (application, error) = await applicationService.CreateAsync(request, cancellationToken);
        if (error is not null)
        {
            return ToErrorResult(error);
        }

        return CreatedAtAction(nameof(GetById), new { id = application!.Id }, application);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApplicationDto>> Update(Guid id, UpdateApplicationRequest request, CancellationToken cancellationToken)
    {
        var (application, error, notFound) = await applicationService.UpdateAsync(id, request, cancellationToken);
        if (notFound)
        {
            return NotFound();
        }

        if (error is not null)
        {
            return ToErrorResult(error);
        }

        return Ok(application);
    }


    private ActionResult ToErrorResult(string error)
    {
        if (error.Contains("already exists", StringComparison.OrdinalIgnoreCase))
        {
            return Conflict(new ProblemDetails
            {
                Title = "Duplicate value",
                Detail = error,
                Status = StatusCodes.Status409Conflict
            });
        }

        return BadRequest(new ProblemDetails
        {
            Title = "Validation error",
            Detail = error,
            Status = StatusCodes.Status400BadRequest
        });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await applicationService.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
