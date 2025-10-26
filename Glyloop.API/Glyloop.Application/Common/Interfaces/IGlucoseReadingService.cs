using Glyloop.Domain.Common;
using Glyloop.Domain.ValueObjects;

namespace Glyloop.Application.Common.Interfaces;

/// <summary>
/// Service for retrieving glucose reading data.
/// Abstracts access to CGM (Continuous Glucose Monitor) data from Dexcom.
/// </summary>
public interface IGlucoseReadingService
{
    /// <summary>
    /// Gets the glucose reading closest to a specific target time.
    /// Used for analyzing event outcomes (e.g., glucose +2 hours after food).
    /// </summary>
    /// <param name="userId">User whose readings to query</param>
    /// <param name="targetTime">Target time to find reading near</param>
    /// <param name="toleranceMinutes">Maximum time difference in minutes (default: 15)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing glucose reading or null if none found within tolerance</returns>
    Task<Result<GlucoseReading?>> GetReadingNearTimeAsync(
        UserId userId,
        DateTimeOffset targetTime,
        int toleranceMinutes = 15,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets glucose readings within a time range.
    /// Used for chart display and time-in-range calculations.
    /// </summary>
    /// <param name="userId">User whose readings to query</param>
    /// <param name="startTime">Start of time range</param>
    /// <param name="endTime">End of time range</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing list of glucose readings</returns>
    Task<Result<IReadOnlyList<GlucoseReading>>> GetReadingsInRangeAsync(
        UserId userId,
        DateTimeOffset startTime,
        DateTimeOffset endTime,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// DTO representing a glucose reading from CGM.
/// </summary>
public record GlucoseReading(
    DateTimeOffset SystemTime,
    int ValueMgDl,
    string? Trend);

