using Glyloop.Domain.Common;
using Glyloop.Domain.Enums;
using Glyloop.Domain.ValueObjects;

namespace Glyloop.Domain.Aggregates.Event.Events;

/// <summary>
/// Domain event raised when an exercise event is created.
/// Reference: DDD Plan Section 4 - Domain Events
/// </summary>
public sealed record ExerciseEventCreatedEvent : DomainEvent
{
    public new Guid EventId { get; init; }
    public Guid UserId { get; init; }
    public DateTimeOffset EventTime { get; init; }
    public int ExerciseTypeId { get; init; }
    public int DurationMinutes { get; init; }
    public IntensityType Intensity { get; init; }
    public string? Note { get; init; }

    public ExerciseEventCreatedEvent(
        Guid eventId,
        UserId userId,
        DateTimeOffset eventTime,
        ExerciseTypeId exerciseType,
        ExerciseDuration duration,
        IntensityType intensity,
        NoteText? note,
        Guid correlationId,
        Guid causationId,
        DateTimeOffset? occurredAt = null)
        : base(correlationId, causationId, occurredAt)
    {
        EventId = eventId;
        UserId = userId;
        EventTime = eventTime;
        ExerciseTypeId = exerciseType;
        DurationMinutes = duration;
        Intensity = intensity;
        Note = note?.Text;
    }
}

