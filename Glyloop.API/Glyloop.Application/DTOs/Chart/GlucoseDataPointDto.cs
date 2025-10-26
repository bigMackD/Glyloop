namespace Glyloop.Application.DTOs.Chart;

/// <summary>
/// DTO representing a single glucose data point for chart display.
/// </summary>
public record GlucoseDataPointDto(
    DateTimeOffset Time,
    int ValueMgDl,
    string? Trend);

