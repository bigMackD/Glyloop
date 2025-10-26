namespace Glyloop.Application.DTOs.Chart;

/// <summary>
/// DTO containing time-in-range calculation results.
/// </summary>
public record TimeInRangeDto(
    decimal? TirPercentage,
    int TotalReadings,
    int InRangeCount,
    int LowerBound,
    int UpperBound);

