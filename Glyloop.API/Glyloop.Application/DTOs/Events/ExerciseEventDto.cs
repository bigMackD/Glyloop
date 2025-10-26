using Glyloop.Domain.Enums;

namespace Glyloop.Application.DTOs.Events;

/// <summary>
/// DTO for exercise/activity events.
/// </summary>
public record ExerciseEventDto(
    Guid EventId,
    Guid UserId,
    EventType EventType,
    DateTimeOffset EventTime,
    DateTimeOffset CreatedAt,
    string? Note,
    int ExerciseTypeId,
    int DurationMinutes,
    IntensityType Intensity) : EventDto(EventId, UserId, EventType, EventTime, CreatedAt, Note);

