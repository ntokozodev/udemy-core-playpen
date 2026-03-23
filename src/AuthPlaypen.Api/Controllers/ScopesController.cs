using AuthPlaypen.Application.Dtos;
using AuthPlaypen.Application.Services;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Mvc;

namespace AuthPlaypen.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ScopesController(IScopeService scopeService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<CursorPagedResultDto<ScopeDto>>> GetPage(
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
        var result = await scopeService.GetPageAsync(parsedCursor, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("search")]
    public async Task<ActionResult<IReadOnlyCollection<ScopeReferenceDto>>> Search(
        [FromQuery] string term,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            return Ok(Array.Empty<ScopeReferenceDto>());
        }

        if (pageSize < 1 || pageSize > 100)
        {
            return BadRequest(new ProblemDetails { Title = "Invalid page size", Detail = "PageSize must be between 1 and 100." });
        }

        var results = await scopeService.SearchAsync(term, pageSize, cancellationToken);
        return Ok(results);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ScopeDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var scope = await scopeService.GetByIdAsync(id, cancellationToken);
        return scope is null ? NotFound() : Ok(scope);
    }

    [HttpPost]
    public async Task<ActionResult<ScopeDto>> Create(CreateScopeRequest request, CancellationToken cancellationToken)
    {
        var (scope, error) = await scopeService.CreateAsync(request, cancellationToken);
        if (error is not null)
        {
            return ToErrorResult(error);
        }

        return CreatedAtAction(nameof(GetById), new { id = scope!.Id }, scope);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ScopeDto>> Update(Guid id, UpdateScopeRequest request, CancellationToken cancellationToken)
    {
        var (scope, error, notFound) = await scopeService.UpdateAsync(id, request, cancellationToken);
        if (notFound)
        {
            return NotFound();
        }

        if (error is not null)
        {
            return ToErrorResult(error);
        }

        return Ok(scope);
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
        var (deleted, _, notFound) = await scopeService.DeleteAsync(id, cancellationToken);
        if (notFound)
        {
            return NotFound();
        }

        return deleted ? NoContent() : NotFound();
    }
}
