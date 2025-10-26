using Glyloop.API.Contracts.Events;
using Glyloop.API.Mapping;
using Glyloop.Application.Common.Interfaces;
using Glyloop.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Glyloop.API.Controllers;

/// <summary>
/// Manages diabetes event logging (food, insulin, exercise, notes).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class EventsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;

    public EventsController(
        IMediator mediator,
        ICurrentUserService currentUserService)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Logs a food intake event with carbohydrate information.
    /// </summary>
    /// <param name="request">Food event details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created food event</returns>
    /// <response code="201">Food event created successfully</response>
    /// <response code="400">Invalid input (future time, invalid carbs, etc.)</response>
    /// <response code="401">User not authenticated</response>
    [HttpPost("food")]
    [ProducesResponseType(typeof(FoodEventResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<FoodEventResponse>> CreateFoodEvent(
        [FromBody] CreateFoodEventRequest request,
        CancellationToken cancellationToken)
    {
        var command = request.ToCommand();
        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Event Creation Failed",
                Detail = result.Error.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }

        var response = result.Value.ToResponse() as FoodEventResponse;
        return CreatedAtAction(nameof(GetEventById), new { id = response!.EventId }, response);
    }

    /// <summary>
    /// Logs an insulin administration event.
    /// </summary>
    /// <param name="request">Insulin event details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created insulin event</returns>
    /// <response code="201">Insulin event created successfully</response>
    /// <response code="400">Invalid input (future time, invalid dose, etc.)</response>
    /// <response code="401">User not authenticated</response>
    [HttpPost("insulin")]
    [ProducesResponseType(typeof(InsulinEventResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<InsulinEventResponse>> CreateInsulinEvent(
        [FromBody] CreateInsulinEventRequest request,
        CancellationToken cancellationToken)
    {
        var command = request.ToCommand();
        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Event Creation Failed",
                Detail = result.Error.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }

        var response = result.Value.ToResponse() as InsulinEventResponse;
        return CreatedAtAction(nameof(GetEventById), new { id = response!.EventId }, response);
    }

    /// <summary>
    /// Logs an exercise activity event.
    /// </summary>
    /// <param name="request">Exercise event details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created exercise event</returns>
    /// <response code="201">Exercise event created successfully</response>
    /// <response code="400">Invalid input (future time, invalid duration, etc.)</response>
    /// <response code="401">User not authenticated</response>
    [HttpPost("exercise")]
    [ProducesResponseType(typeof(ExerciseEventResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ExerciseEventResponse>> CreateExerciseEvent(
        [FromBody] CreateExerciseEventRequest request,
        CancellationToken cancellationToken)
    {
        var command = request.ToCommand();
        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Event Creation Failed",
                Detail = result.Error.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }

        var response = result.Value.ToResponse() as ExerciseEventResponse;
        return CreatedAtAction(nameof(GetEventById), new { id = response!.EventId }, response);
    }

    /// <summary>
    /// Logs a free-text note event.
    /// </summary>
    /// <param name="request">Note event details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created note event</returns>
    /// <response code="201">Note event created successfully</response>
    /// <response code="400">Invalid input (future time, empty note, etc.)</response>
    /// <response code="401">User not authenticated</response>
    [HttpPost("note")]
    [ProducesResponseType(typeof(NoteEventResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<NoteEventResponse>> CreateNoteEvent(
        [FromBody] CreateNoteEventRequest request,
        CancellationToken cancellationToken)
    {
        var command = request.ToCommand();
        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Event Creation Failed",
                Detail = result.Error.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }

        var response = result.Value.ToResponse() as NoteEventResponse;
        return CreatedAtAction(nameof(GetEventById), new { id = response!.EventId }, response);
    }

    /// <summary>
    /// Retrieves a paginated list of events with optional filtering.
    /// </summary>
    /// <param name="eventType">Filter by event type (Food, Insulin, Exercise, Note)</param>
    /// <param name="fromDate">Start of date range filter</param>
    /// <param name="toDate">End of date range filter</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 20, max: 100)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of events</returns>
    /// <response code="200">Events retrieved successfully</response>
    /// <response code="400">Invalid query parameters</response>
    /// <response code="401">User not authenticated</response>
    [HttpGet]
    [ProducesResponseType(typeof(Contracts.Common.PagedResponse<EventListItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Contracts.Common.PagedResponse<EventListItemResponse>>> ListEvents(
        [FromQuery] EventType? eventType = null,
        [FromQuery] DateTimeOffset? fromDate = null,
        [FromQuery] DateTimeOffset? toDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = eventType.ToQuery(fromDate, toDate, page, pageSize);
        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Query Failed",
                Detail = result.Error.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }

        var response = result.Value.ToPagedResponse();
        return Ok(response);
    }

    /// <summary>
    /// Retrieves a specific event by ID.
    /// </summary>
    /// <param name="id">Event unique identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Event details</returns>
    /// <response code="200">Event found</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="404">Event not found</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventResponse>> GetEventById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var query = id.ToQuery();
        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Event Not Found",
                Detail = result.Error.Message,
                Status = StatusCodes.Status404NotFound
            });
        }

        var response = result.Value.ToResponse();
        return Ok(response);
    }

    /// <summary>
    /// Gets the +2h glucose outcome for a food event.
    /// Shows glucose value approximately 2 hours after the meal.
    /// </summary>
    /// <param name="id">Food event unique identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Outcome with glucose value or N/A if no reading available</returns>
    /// <response code="200">Outcome retrieved (may contain N/A)</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="404">Event not found or not a food event</response>
    [HttpGet("{id:guid}/outcome")]
    [ProducesResponseType(typeof(EventOutcomeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventOutcomeResponse>> GetEventOutcome(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var query = id.ToOutcomeQuery();
        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Outcome Not Available",
                Detail = result.Error.Message,
                Status = StatusCodes.Status404NotFound
            });
        }

        var response = result.Value.ToResponse();
        return Ok(response);
    }
}

