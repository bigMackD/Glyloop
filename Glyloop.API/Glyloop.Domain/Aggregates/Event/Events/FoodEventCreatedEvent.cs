using Glyloop.Domain.Common;
using Glyloop.Domain.Enums;
using Glyloop.Domain.ValueObjects;

namespace Glyloop.Domain.Aggregates.Event.Events;

/// <summary>
/// Domain event raised when a food event is created.
/// Reference: DDD Plan Section 4 - Domain Events
/// </summary>
public sealed record FoodEventCreatedEvent : DomainEvent
{
    public new Guid EventId { get; init; }
    public Guid UserId { get; init; }
    public DateTimeOffset EventTime { get; init; }
    public int Carbohydrates { get; init; }
    public int MealTagId { get; init; }
    public AbsorptionHint AbsorptionHint { get; init; }
    public string? Note { get; init; }

    public FoodEventCreatedEvent(
        Guid eventId,
        UserId userId,
        DateTimeOffset eventTime,
        Carbohydrate carbohydrates,
        MealTagId mealTag,
        AbsorptionHint absorptionHint,
        NoteText? note,
        Guid correlationId,
        Guid causationId,
        DateTimeOffset? occurredAt = null)
        : base(correlationId, causationId, occurredAt)
    {
        EventId = eventId;
        UserId = userId;
        EventTime = eventTime;
        Carbohydrates = carbohydrates;
        MealTagId = mealTag;
        AbsorptionHint = absorptionHint;
        Note = note?.Text;
    }
}

