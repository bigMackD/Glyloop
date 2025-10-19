using Glyloop.Domain.Common;
using Glyloop.Domain.ValueObjects;

namespace Glyloop.Domain.Aggregates.DexcomLink.Events;

/// <summary>
/// Domain event raised when a Dexcom account is successfully linked to a user.
/// Reference: DDD Plan Section 4 - Domain Events
/// </summary>
public sealed record DexcomLinkedEvent : DomainEvent
{
    public Guid UserId { get; init; }
    public Guid LinkId { get; init; }

    public DexcomLinkedEvent(
        UserId userId,
        Guid linkId,
        Guid correlationId,
        Guid causationId,
        DateTimeOffset? occurredAt = null)
        : base(correlationId, causationId, occurredAt)
    {
        UserId = userId;
        LinkId = linkId;
    }
}

