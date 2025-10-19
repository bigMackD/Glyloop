using Glyloop.Domain.Common;
using Glyloop.Domain.ValueObjects;

namespace Glyloop.Domain.Aggregates.DexcomLink.Events;

/// <summary>
/// Domain event raised when a Dexcom account is unlinked from a user.
/// Reference: DDD Plan Section 4 - Domain Events
/// </summary>
public sealed record DexcomUnlinkedEvent : DomainEvent
{
    public Guid UserId { get; init; }
    public Guid LinkId { get; init; }
    public bool DataPurged { get; init; }

    public DexcomUnlinkedEvent(
        UserId userId,
        Guid linkId,
        bool dataPurged,
        Guid correlationId,
        Guid causationId,
        DateTimeOffset? occurredAt = null)
        : base(correlationId, causationId, occurredAt)
    {
        UserId = userId;
        LinkId = linkId;
        DataPurged = dataPurged;
    }
}

