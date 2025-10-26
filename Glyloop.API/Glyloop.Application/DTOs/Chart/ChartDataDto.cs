namespace Glyloop.Application.DTOs.Chart;

/// <summary>
/// DTO containing glucose data and event overlays for chart display.
/// </summary>
public record ChartDataDto(
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    IReadOnlyList<GlucoseDataPointDto> GlucoseData,
    IReadOnlyList<EventOverlayDto> Events);

