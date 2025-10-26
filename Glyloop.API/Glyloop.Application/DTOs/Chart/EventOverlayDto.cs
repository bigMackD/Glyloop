using Glyloop.Domain.Enums;

namespace Glyloop.Application.DTOs.Chart;

/// <summary>
/// DTO representing an event marker for chart overlay.
/// Provides event information and tooltips for user actions (food, insulin, exercise, notes).
/// </summary>
public record EventOverlayDto(
    Guid EventId,
    EventType EventType,
    DateTimeOffset EventTime,
    string Tooltip);

