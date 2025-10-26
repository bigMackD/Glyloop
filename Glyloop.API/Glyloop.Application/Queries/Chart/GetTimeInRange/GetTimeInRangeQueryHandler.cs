using Glyloop.Application.Common.Interfaces;
using Glyloop.Application.DTOs.Chart;
using Glyloop.Domain.Common;
using Glyloop.Domain.ValueObjects;
using MediatR;

namespace Glyloop.Application.Queries.Chart.GetTimeInRange;

/// <summary>
/// Handler for GetTimeInRangeQuery.
/// Calculates the percentage of glucose readings within the user's target range.
/// </summary>
public class GetTimeInRangeQueryHandler : IRequestHandler<GetTimeInRangeQuery, Result<TimeInRangeDto>>
{
    private readonly IGlucoseReadingService _glucoseReadingService;
    private readonly IIdentityService _identityService;
    private readonly ICurrentUserService _currentUserService;

    public GetTimeInRangeQueryHandler(
        IGlucoseReadingService glucoseReadingService,
        IIdentityService identityService,
        ICurrentUserService currentUserService)
    {
        _glucoseReadingService = glucoseReadingService;
        _identityService = identityService;
        _currentUserService = currentUserService;
    }

    public async Task<Result<TimeInRangeDto>> Handle(
        GetTimeInRangeQuery request,
        CancellationToken cancellationToken)
    {
        var userId = UserId.Create(_currentUserService.UserId);

        var tirRangeResult = await _identityService.GetUserPreferencesAsync(userId, cancellationToken);
        if (tirRangeResult.IsFailure)
        {
            return Result.Failure<TimeInRangeDto>(tirRangeResult.Error);
        }

        var tirRange = tirRangeResult.Value;

        var readingsResult = await _glucoseReadingService.GetReadingsInRangeAsync(
            userId,
            request.FromTime,
            request.ToTime,
            cancellationToken);

        if (readingsResult.IsFailure)
        {
            return Result.Failure<TimeInRangeDto>(readingsResult.Error);
        }

        var readings = readingsResult.Value;
        var totalReadings = readings.Count;

        var inRangeCount = readings.Count(r => tirRange.IsInRange(r.ValueMgDl));

        decimal? tirPercentage = totalReadings > 0
            ? Math.Round((decimal)inRangeCount / totalReadings * 100, 1)
            : null;

        var dto = new TimeInRangeDto(
            tirPercentage,
            totalReadings,
            inRangeCount,
            tirRange.Lower,
            tirRange.Upper);

        return Result.Success(dto);
    }
}

