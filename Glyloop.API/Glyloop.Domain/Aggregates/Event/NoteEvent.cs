using Glyloop.Domain.Aggregates.Event.Events;
using Glyloop.Domain.Common;
using Glyloop.Domain.Enums;
using Glyloop.Domain.ValueObjects;

namespace Glyloop.Domain.Aggregates.Event;

/// <summary>
/// Represents a standalone note or annotation event.
/// Used for general observations, symptoms, or context not tied to specific actions.
/// 
/// Reference: DDD Plan Section 2 - Aggregates (Event), Section 5 - Commands (AddNoteEventCommand)
/// </summary>
public sealed class NoteEvent : Event
{
    /// <summary>
    /// Gets the note text content.
    /// For NoteEvent, this is the primary data (unlike other events where Note is optional).
    /// </summary>
    public NoteText Text { get; private set; }

    // EF Core constructor
    private NoteEvent() : base()
    {
        Text = null!;
    }

    private NoteEvent(
        Guid id,
        UserId userId,
        DateTimeOffset eventTime,
        SourceType source,
        NoteText text,
        DateTimeOffset createdAt)
        : base(id, userId, eventTime, EventType.Note, source, null, createdAt)
    {
        Text = text;
    }

    /// <summary>
    /// Creates a new note event.
    /// </summary>
    /// <param name="userId">User creating the event</param>
    /// <param name="eventTime">When the observation was made</param>
    /// <param name="text">Note content</param>
    /// <param name="source">Event source (Manual, Imported, System)</param>
    /// <param name="timeProvider">Time provider for validation and timestamp</param>
    /// <param name="correlationId">Correlation ID for event tracking</param>
    /// <param name="causationId">Causation ID for event tracking</param>
    /// <returns>Result containing NoteEvent or validation error</returns>
    public static Result<NoteEvent> Create(
        UserId userId,
        DateTimeOffset eventTime,
        NoteText text,
        SourceType source,
        ITimeProvider timeProvider,
        Guid correlationId,
        Guid causationId)
    {
        // Validate event time
        var eventTimeValidation = ValidateEventTime(eventTime, timeProvider);
        if (eventTimeValidation.IsFailure)
            return Result.Failure<NoteEvent>(eventTimeValidation.Error);

        var now = timeProvider.UtcNow;
        var eventId = Guid.NewGuid();

        var noteEvent = new NoteEvent(
            eventId,
            userId,
            eventTime,
            source,
            text,
            now);

        // Raise domain event
        noteEvent.RaiseDomainEvent(new NoteEventCreatedEvent(
            eventId,
            userId,
            eventTime,
            text,
            correlationId,
            causationId,
            now));

        return Result.Success(noteEvent);
    }
}

