namespace Glyloop.Domain.Common;

/// <summary>
/// Marker interface for domain events.
/// Domain events represent something meaningful that happened in the domain.
/// They are used to communicate between bounded contexts and trigger side effects.
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// Gets the unique identifier for this event.
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// Gets the date and time when this event occurred (UTC).
    /// </summary>
    DateTimeOffset OccurredAt { get; }

    /// <summary>
    /// Gets the correlation ID for tracking related events across the system.
    /// </summary>
    Guid CorrelationId { get; }

    /// <summary>
    /// Gets the causation ID - the ID of the event or command that caused this event.
    /// </summary>
    Guid CausationId { get; }
}

