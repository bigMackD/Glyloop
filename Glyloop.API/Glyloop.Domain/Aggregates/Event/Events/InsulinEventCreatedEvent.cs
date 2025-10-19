using Glyloop.Domain.Common;
using Glyloop.Domain.Enums;
using Glyloop.Domain.ValueObjects;

namespace Glyloop.Domain.Aggregates.Event.Events;

/// <summary>
/// Domain event raised when an insulin event is created.
/// Reference: DDD Plan Section 4 - Domain Events
/// </summary>
public sealed record InsulinEventCreatedEvent : DomainEvent
{
    public new Guid EventId { get; init; }
    public Guid UserId { get; init; }
    public DateTimeOffset EventTime { get; init; }
    public InsulinType InsulinType { get; init; }
    public decimal Dose { get; init; }
    public string? Preparation { get; init; }
    public string? Delivery { get; init; }
    public string? Timing { get; init; }
    public string? Note { get; init; }

    public InsulinEventCreatedEvent(
        Guid eventId,
        UserId userId,
        DateTimeOffset eventTime,
        InsulinType insulinType,
        InsulinDose dose,
        string? preparation,
        string? delivery,
        string? timing,
        NoteText? note,
        Guid correlationId,
        Guid causationId,
        DateTimeOffset? occurredAt = null)
        : base(correlationId, causationId, occurredAt)
    {
        EventId = eventId;
        UserId = userId;
        EventTime = eventTime;
        InsulinType = insulinType;
        Dose = dose;
        Preparation = preparation;
        Delivery = delivery;
        Timing = timing;
        Note = note?.Text;
    }
}

