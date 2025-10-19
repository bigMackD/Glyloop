namespace Glyloop.Domain.Common;

/// <summary>
/// Base class for domain events with common metadata properties.
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    public Guid EventId { get; init; }
    public DateTimeOffset OccurredAt { get; init; }
    public Guid CorrelationId { get; init; }
    public Guid CausationId { get; init; }

    protected DomainEvent(Guid correlationId, Guid causationId, DateTimeOffset? occurredAt = null)
    {
        EventId = Guid.NewGuid();
        OccurredAt = occurredAt ?? DateTimeOffset.UtcNow;
        CorrelationId = correlationId;
        CausationId = causationId;
    }
}

