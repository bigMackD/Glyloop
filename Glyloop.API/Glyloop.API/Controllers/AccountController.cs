using Glyloop.API.Contracts.Account;
using Glyloop.API.Mapping;
using Glyloop.Application.Common.Interfaces;
using Glyloop.Application.Queries.Account.GetUserPreferences;
using Glyloop.Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Glyloop.API.Controllers;

/// <summary>
/// Manages user account preferences and settings.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class AccountController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;

    public AccountController(
        IMediator mediator,
        ICurrentUserService currentUserService)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Gets the current user's Time in Range (TIR) preferences.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User's TIR preferences</returns>
    /// <response code="200">Preferences retrieved successfully</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="404">User preferences not found</response>
    [HttpGet("preferences")]
    [ProducesResponseType(typeof(PreferencesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PreferencesResponse>> GetPreferences(
        CancellationToken cancellationToken)
    {
        var query = new GetUserPreferencesQuery();
        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Preferences Not Found",
                Detail = result.Error.Message,
                Status = StatusCodes.Status404NotFound
            });
        }

        var response = result.Value.ToResponse();
        return Ok(response);
    }

    /// <summary>
    /// Updates the current user's Time in Range (TIR) preferences.
    /// </summary>
    /// <param name="request">Updated TIR preferences</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content on success</returns>
    /// <response code="200">Preferences updated successfully</response>
    /// <response code="400">Invalid TIR range (lower must be less than upper)</response>
    /// <response code="401">User not authenticated</response>
    [HttpPut("preferences")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdatePreferences(
        [FromBody] UpdatePreferencesRequest request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        var command = request.ToCommand(userId);
        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Update Failed",
                Detail = result.Error.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }

        return Ok(new { message = "Preferences updated successfully" });
    }
}

