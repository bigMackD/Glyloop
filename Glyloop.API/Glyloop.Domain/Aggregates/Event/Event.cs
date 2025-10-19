using Glyloop.Domain.Common;
using Glyloop.Domain.Enums;
using Glyloop.Domain.Errors;
using Glyloop.Domain.ValueObjects;

namespace Glyloop.Domain.Aggregates.Event;

/// <summary>
/// Abstract base class for all user events in the system.
/// Events are immutable domain objects representing user actions or observations
/// (Food intake, Insulin administration, Exercise, Notes).
/// 
/// Invariants:
/// - EventTime must be less than or equal to current time (no future events)
/// - Events are immutable after creation (no update methods)
/// - Each event must have a valid UserId
/// - Subtype-specific invariants are enforced by value objects
/// 
/// Reference: DDD Plan Section 2 - Aggregates (Event)
/// 
/// Infrastructure mapping notes:
/// - Use Table-Per-Type (TPT) with discriminator for Event subtypes in EF Core
/// - Each subtype (FoodEvent, InsulinEvent, etc.) gets its own table
/// - EventTime should be stored as DATETIMEOFFSET
/// - Index on UserId, EventTime, EventType for query performance
/// - Base Event table contains common properties; subtype tables contain specific properties
/// </summary>
public abstract class Event : AggregateRoot<Guid>
{
    /// <summary>
    /// Gets the user who created this event.
    /// </summary>
    public UserId UserId { get; private set; }

    /// <summary>
    /// Gets when this event occurred (user time, stored as UTC offset).
    /// </summary>
    public DateTimeOffset EventTime { get; private set; }

    /// <summary>
    /// Gets the type of event (Food, Insulin, Exercise, Note).
    /// Used as discriminator for persistence.
    /// </summary>
    public EventType EventType { get; private set; }

    /// <summary>
    /// Gets the source/origin of this event.
    /// </summary>
    public SourceType Source { get; private set; }

    /// <summary>
    /// Optional note text attached to this event.
    /// All event types support optional notes for additional context.
    /// </summary>
    public NoteText? Note { get; private set; }

    /// <summary>
    /// Gets when this event was created in the system.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    // EF Core constructor
    protected Event() : base(Guid.Empty)
    {
        UserId = null!;
    }

    protected Event(
        Guid id,
        UserId userId,
        DateTimeOffset eventTime,
        EventType eventType,
        SourceType source,
        NoteText? note,
        DateTimeOffset createdAt) : base(id)
    {
        UserId = userId;
        EventTime = eventTime;
        EventType = eventType;
        Source = source;
        Note = note;
        CreatedAt = createdAt;
    }

    /// <summary>
    /// Validates that the event time is not in the future.
    /// Called by all subtype factory methods.
    /// </summary>
    protected static Result ValidateEventTime(DateTimeOffset eventTime, ITimeProvider timeProvider)
    {
        if (eventTime > timeProvider.UtcNow)
            return Result.Failure(DomainErrors.Event.EventInFuture);

        return Result.Success();
    }
}

