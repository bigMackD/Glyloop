using Glyloop.API.Contracts.Chart;
using Glyloop.API.Mapping;
using Glyloop.Application.Common.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Glyloop.API.Controllers;

/// <summary>
/// Provides glucose chart data and Time in Range statistics.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class ChartController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;

    public ChartController(
        IMediator mediator,
        ICurrentUserService currentUserService)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Retrieves glucose data and event overlays for chart visualization.
    /// </summary>
    /// <param name="range">Time range (1h, 3h, 5h, 8h, 12h, 24h). Default: 3h</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Glucose data points and event overlay markers</returns>
    /// <response code="200">Chart data retrieved successfully</response>
    /// <response code="400">Invalid time range</response>
    /// <response code="401">User not authenticated</response>
    /// <remarks>
    /// Supported time ranges:
    /// - 1: Last 1 hour
    /// - 3: Last 3 hours (default)
    /// - 5: Last 5 hours
    /// - 8: Last 8 hours
    /// - 12: Last 12 hours
    /// - 24: Last 24 hours
    /// 
    /// Gaps in glucose data are shown as breaks in the chart.
    /// Y-axis is dynamically clamped to [50, 350] mg/dL.
    /// No smoothing is applied to glucose values.
    /// </remarks>
    [HttpGet("data")]
    [ProducesResponseType(typeof(ChartDataResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ChartDataResponse>> GetChartData(
        [FromQuery] string range = "3",
        CancellationToken cancellationToken = default)
    {
        var query = range.ToQuery();
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

        var response = result.Value.ToResponse();
        return Ok(response);
    }

    /// <summary>
    /// Calculates Time in Range (TIR) statistics for the specified time window.
    /// </summary>
    /// <param name="range">Time range in hours (1, 3, 5, 8, 12, 24). Default: 3</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>TIR percentage and detailed readings breakdown</returns>
    /// <response code="200">TIR statistics calculated successfully</response>
    /// <response code="400">Invalid time range</response>
    /// <response code="401">User not authenticated</response>
    /// <remarks>
    /// TIR is calculated using the user's configured target range (default: 70-180 mg/dL).
    /// Missing CGM intervals are excluded from the denominator.
    /// 
    /// Example response:
    /// - TimeInRangePercentage: 75.5 (% of readings in target range)
    /// - TotalReadings: 60 (number of glucose readings in time window)
    /// - ReadingsInRange: 45 (readings within target range)
    /// - ReadingsBelowRange: 5 (readings below target)
    /// - ReadingsAboveRange: 10 (readings above target)
    /// </remarks>
    [HttpGet("tir")]
    [ProducesResponseType(typeof(TimeInRangeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TimeInRangeResponse>> GetTimeInRange(
        [FromQuery] string range = "3",
        CancellationToken cancellationToken = default)
    {
        var query = range.ToTirQuery();
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

        var response = result.Value.ToResponse();
        return Ok(response);
    }
}

