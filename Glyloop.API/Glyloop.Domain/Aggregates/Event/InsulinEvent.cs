using Glyloop.Domain.Aggregates.Event.Events;
using Glyloop.Domain.Common;
using Glyloop.Domain.Enums;
using Glyloop.Domain.ValueObjects;

namespace Glyloop.Domain.Aggregates.Event;

/// <summary>
/// Represents an insulin administration event.
/// Tracks insulin doses for both fast-acting (bolus) and long-acting (basal) insulin.
/// 
/// Reference: DDD Plan Section 2 - Aggregates (Event), Section 5 - Commands (AddInsulinEventCommand)
/// </summary>
public sealed class InsulinEvent : Event
{
    /// <summary>
    /// Gets the type of insulin administered (Fast or Long).
    /// </summary>
    public InsulinType InsulinType { get; private set; }

    /// <summary>
    /// Gets the dose amount in units.
    /// </summary>
    public InsulinDose Dose { get; private set; }

    /// <summary>
    /// Gets optional insulin preparation details (e.g., brand, pen ID).
    /// </summary>
    public string? Preparation { get; private set; }

    /// <summary>
    /// Gets optional delivery method details (e.g., injection site, pump settings).
    /// </summary>
    public string? Delivery { get; private set; }

    /// <summary>
    /// Gets optional timing context (e.g., "Before meal", "After meal", "Bedtime").
    /// </summary>
    public string? Timing { get; private set; }

    // EF Core constructor
    private InsulinEvent() : base()
    {
        Dose = null!;
    }

    private InsulinEvent(
        Guid id,
        UserId userId,
        DateTimeOffset eventTime,
        SourceType source,
        InsulinType insulinType,
        InsulinDose dose,
        string? preparation,
        string? delivery,
        string? timing,
        NoteText? note,
        DateTimeOffset createdAt)
        : base(id, userId, eventTime, EventType.Insulin, source, note, createdAt)
    {
        InsulinType = insulinType;
        Dose = dose;
        Preparation = preparation;
        Delivery = delivery;
        Timing = timing;
    }

    /// <summary>
    /// Creates a new insulin event.
    /// </summary>
    /// <param name="userId">User creating the event</param>
    /// <param name="eventTime">When insulin was administered</param>
    /// <param name="insulinType">Type of insulin (Fast or Long)</param>
    /// <param name="dose">Dose in units</param>
    /// <param name="preparation">Optional preparation details</param>
    /// <param name="delivery">Optional delivery method</param>
    /// <param name="timing">Optional timing context</param>
    /// <param name="note">Optional note</param>
    /// <param name="source">Event source (Manual, Imported, System)</param>
    /// <param name="timeProvider">Time provider for validation and timestamp</param>
    /// <param name="correlationId">Correlation ID for event tracking</param>
    /// <param name="causationId">Causation ID for event tracking</param>
    /// <returns>Result containing InsulinEvent or validation error</returns>
    public static Result<InsulinEvent> Create(
        UserId userId,
        DateTimeOffset eventTime,
        InsulinType insulinType,
        InsulinDose dose,
        string? preparation,
        string? delivery,
        string? timing,
        NoteText? note,
        SourceType source,
        ITimeProvider timeProvider,
        Guid correlationId,
        Guid causationId)
    {
        // Validate event time
        var eventTimeValidation = ValidateEventTime(eventTime, timeProvider);
        if (eventTimeValidation.IsFailure)
            return Result.Failure<InsulinEvent>(eventTimeValidation.Error);

        var now = timeProvider.UtcNow;
        var eventId = Guid.NewGuid();

        var insulinEvent = new InsulinEvent(
            eventId,
            userId,
            eventTime,
            source,
            insulinType,
            dose,
            preparation,
            delivery,
            timing,
            note,
            now);

        // Raise domain event
        insulinEvent.RaiseDomainEvent(new InsulinEventCreatedEvent(
            eventId,
            userId,
            eventTime,
            insulinType,
            dose,
            preparation,
            delivery,
            timing,
            note,
            correlationId,
            causationId,
            now));

        return Result.Success(insulinEvent);
    }
}

