using Glyloop.Application.DTOs.Chart;
using Glyloop.Domain.Common;
using MediatR;

namespace Glyloop.Application.Queries.Chart.GetTimeInRange;

/// <summary>
/// Query to calculate time-in-range percentage for a given time window.
/// Uses the user's configured TIR range preferences.
/// </summary>
public record GetTimeInRangeQuery(
    DateTimeOffset FromTime,
    DateTimeOffset ToTime) : IRequest<Result<TimeInRangeDto>>;

