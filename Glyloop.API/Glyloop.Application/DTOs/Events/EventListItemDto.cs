using Glyloop.Domain.Enums;

namespace Glyloop.Application.DTOs.Events;

/// <summary>
/// Lightweight DTO for event list items.
/// Used in paginated event history queries.
/// </summary>
public record EventListItemDto(
    Guid EventId,
    EventType EventType,
    DateTimeOffset EventTime,
    string Summary);

