using Glyloop.Domain.Aggregates.Event.Events;
using Glyloop.Domain.Common;
using Glyloop.Domain.Enums;
using Glyloop.Domain.ValueObjects;

namespace Glyloop.Domain.Aggregates.Event;

/// <summary>
/// Represents a food intake event with carbohydrate information.
/// Used for tracking meals, snacks, and carbohydrate consumption.
/// 
/// Reference: DDD Plan Section 2 - Aggregates (Event), Section 5 - Commands (AddFoodEventCommand)
/// </summary>
public sealed class FoodEvent : Event
{
    /// <summary>
    /// Gets the carbohydrate content of the food consumed.
    /// </summary>
    public Carbohydrate Carbohydrates { get; private set; }

    /// <summary>
    /// Gets the meal category (Breakfast, Lunch, Dinner, Snack, etc.).
    /// </summary>
    public MealTagId MealTag { get; private set; }

    /// <summary>
    /// Gets the expected absorption rate of the food.
    /// </summary>
    public AbsorptionHint AbsorptionHint { get; private set; }

    // EF Core constructor
    private FoodEvent() : base()
    {
        Carbohydrates = null!;
        MealTag = null!;
    }

    private FoodEvent(
        Guid id,
        UserId userId,
        DateTimeOffset eventTime,
        SourceType source,
        Carbohydrate carbohydrates,
        MealTagId mealTag,
        AbsorptionHint absorptionHint,
        NoteText? note,
        DateTimeOffset createdAt)
        : base(id, userId, eventTime, EventType.Food, source, note, createdAt)
    {
        Carbohydrates = carbohydrates;
        MealTag = mealTag;
        AbsorptionHint = absorptionHint;
    }

    /// <summary>
    /// Creates a new food event.
    /// </summary>
    /// <param name="userId">User creating the event</param>
    /// <param name="eventTime">When the food was consumed</param>
    /// <param name="carbohydrates">Carbohydrate content</param>
    /// <param name="mealTag">Meal category</param>
    /// <param name="absorptionHint">Expected absorption rate</param>
    /// <param name="note">Optional note</param>
    /// <param name="source">Event source (Manual, Imported, System)</param>
    /// <param name="timeProvider">Time provider for validation and timestamp</param>
    /// <param name="correlationId">Correlation ID for event tracking</param>
    /// <param name="causationId">Causation ID for event tracking</param>
    /// <returns>Result containing FoodEvent or validation error</returns>
    public static Result<FoodEvent> Create(
        UserId userId,
        DateTimeOffset eventTime,
        Carbohydrate carbohydrates,
        MealTagId mealTag,
        AbsorptionHint absorptionHint,
        NoteText? note,
        SourceType source,
        ITimeProvider timeProvider,
        Guid correlationId,
        Guid causationId)
    {
        // Validate event time
        var eventTimeValidation = ValidateEventTime(eventTime, timeProvider);
        if (eventTimeValidation.IsFailure)
            return Result.Failure<FoodEvent>(eventTimeValidation.Error);

        var now = timeProvider.UtcNow;
        var eventId = Guid.NewGuid();

        var foodEvent = new FoodEvent(
            eventId,
            userId,
            eventTime,
            source,
            carbohydrates,
            mealTag,
            absorptionHint,
            note,
            now);

        // Raise domain event
        foodEvent.RaiseDomainEvent(new FoodEventCreatedEvent(
            eventId,
            userId,
            eventTime,
            carbohydrates,
            mealTag,
            absorptionHint,
            note,
            correlationId,
            causationId,
            now));

        return Result.Success(foodEvent);
    }
}

