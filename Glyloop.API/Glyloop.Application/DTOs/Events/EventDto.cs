using Glyloop.Domain.Enums;

namespace Glyloop.Application.DTOs.Events;

/// <summary>
/// Base DTO for all event types containing common properties.
/// </summary>
public abstract record EventDto(
    Guid EventId,
    Guid UserId,
    EventType EventType,
    DateTimeOffset EventTime,
    DateTimeOffset CreatedAt,
    string? Note);

