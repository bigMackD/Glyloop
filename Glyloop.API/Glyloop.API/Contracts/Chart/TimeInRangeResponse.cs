namespace Glyloop.API.Contracts.Chart;

/// <summary>
/// Response containing Time in Range statistics for a time window.
/// </summary>
public record TimeInRangeResponse(
    decimal TimeInRangePercentage,
    int TotalReadings,
    int ReadingsInRange,
    int ReadingsBelowRange,
    int ReadingsAboveRange,
    int TargetLowerBound,
    int TargetUpperBound,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime);

