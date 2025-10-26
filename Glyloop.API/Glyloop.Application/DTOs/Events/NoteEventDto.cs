using Glyloop.Domain.Enums;

namespace Glyloop.Application.DTOs.Events;

/// <summary>
/// DTO for standalone note/annotation events.
/// </summary>
public record NoteEventDto(
    Guid EventId,
    Guid UserId,
    EventType EventType,
    DateTimeOffset EventTime,
    DateTimeOffset CreatedAt,
    string? Note,
    string Text) : EventDto(EventId, UserId, EventType, EventTime, CreatedAt, Note);

