namespace Glyloop.API.Contracts.Chart;

/// <summary>
/// Response containing glucose data and event overlays for chart display.
/// </summary>
public record ChartDataResponse(
    IReadOnlyList<GlucoseDataPoint> GlucoseData,
    IReadOnlyList<EventOverlay> EventOverlays,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime);

/// <summary>
/// Individual glucose data point.
/// </summary>
public record GlucoseDataPoint(
    DateTimeOffset Timestamp,
    int Value);

/// <summary>
/// Event overlay marker for chart.
/// </summary>
public record EventOverlay(
    Guid EventId,
    string EventType,
    DateTimeOffset Timestamp,
    string? Icon,
    string? Color,
    string Summary);

