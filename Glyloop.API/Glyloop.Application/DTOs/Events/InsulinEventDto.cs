using Glyloop.Domain.Enums;

namespace Glyloop.Application.DTOs.Events;

/// <summary>
/// DTO for insulin administration events.
/// </summary>
public record InsulinEventDto(
    Guid EventId,
    Guid UserId,
    EventType EventType,
    DateTimeOffset EventTime,
    DateTimeOffset CreatedAt,
    string? Note,
    InsulinType InsulinType,
    decimal Units,
    string? Preparation,
    string? Delivery,
    string? Timing) : EventDto(EventId, UserId, EventType, EventTime, CreatedAt, Note);

