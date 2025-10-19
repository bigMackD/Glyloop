using Glyloop.Domain.Aggregates.Event.Events;
using Glyloop.Domain.Common;
using Glyloop.Domain.Enums;
using Glyloop.Domain.ValueObjects;

namespace Glyloop.Domain.Aggregates.Event;

/// <summary>
/// Represents a physical activity/exercise event.
/// Tracks exercise sessions with type, duration, and intensity for glucose impact analysis.
/// 
/// Reference: DDD Plan Section 2 - Aggregates (Event), Section 5 - Commands (AddExerciseEventCommand)
/// </summary>
public sealed class ExerciseEvent : Event
{
    /// <summary>
    /// Gets the type of exercise performed (Walking, Running, Cycling, etc.).
    /// </summary>
    public ExerciseTypeId ExerciseType { get; private set; }

    /// <summary>
    /// Gets the duration of the exercise session.
    /// </summary>
    public ExerciseDuration Duration { get; private set; }

    /// <summary>
    /// Gets the intensity level of the exercise.
    /// </summary>
    public IntensityType Intensity { get; private set; }

    // EF Core constructor
    private ExerciseEvent() : base()
    {
        ExerciseType = null!;
        Duration = null!;
    }

    private ExerciseEvent(
        Guid id,
        UserId userId,
        DateTimeOffset eventTime,
        SourceType source,
        ExerciseTypeId exerciseType,
        ExerciseDuration duration,
        IntensityType intensity,
        NoteText? note,
        DateTimeOffset createdAt)
        : base(id, userId, eventTime, EventType.Exercise, source, note, createdAt)
    {
        ExerciseType = exerciseType;
        Duration = duration;
        Intensity = intensity;
    }

    /// <summary>
    /// Creates a new exercise event.
    /// </summary>
    /// <param name="userId">User creating the event</param>
    /// <param name="eventTime">When the exercise occurred (start time)</param>
    /// <param name="exerciseType">Type of exercise</param>
    /// <param name="duration">Duration of exercise session</param>
    /// <param name="intensity">Intensity level</param>
    /// <param name="note">Optional note</param>
    /// <param name="source">Event source (Manual, Imported, System)</param>
    /// <param name="timeProvider">Time provider for validation and timestamp</param>
    /// <param name="correlationId">Correlation ID for event tracking</param>
    /// <param name="causationId">Causation ID for event tracking</param>
    /// <returns>Result containing ExerciseEvent or validation error</returns>
    public static Result<ExerciseEvent> Create(
        UserId userId,
        DateTimeOffset eventTime,
        ExerciseTypeId exerciseType,
        ExerciseDuration duration,
        IntensityType intensity,
        NoteText? note,
        SourceType source,
        ITimeProvider timeProvider,
        Guid correlationId,
        Guid causationId)
    {
        // Validate event time
        var eventTimeValidation = ValidateEventTime(eventTime, timeProvider);
        if (eventTimeValidation.IsFailure)
            return Result.Failure<ExerciseEvent>(eventTimeValidation.Error);

        var now = timeProvider.UtcNow;
        var eventId = Guid.NewGuid();

        var exerciseEvent = new ExerciseEvent(
            eventId,
            userId,
            eventTime,
            source,
            exerciseType,
            duration,
            intensity,
            note,
            now);

        // Raise domain event
        exerciseEvent.RaiseDomainEvent(new ExerciseEventCreatedEvent(
            eventId,
            userId,
            eventTime,
            exerciseType,
            duration,
            intensity,
            note,
            correlationId,
            causationId,
            now));

        return Result.Success(exerciseEvent);
    }
}

