using Glyloop.Domain.Common;

namespace Glyloop.Domain.Aggregates.DexcomLink.Events;

/// <summary>
/// Domain event raised when Dexcom access tokens are refreshed.
/// </summary>
public sealed record DexcomTokensRefreshedEvent : DomainEvent
{
    public Guid LinkId { get; init; }
    public DateTimeOffset NewExpiresAt { get; init; }

    public DexcomTokensRefreshedEvent(
        Guid linkId,
        DateTimeOffset newExpiresAt,
        Guid correlationId,
        Guid causationId,
        DateTimeOffset? occurredAt = null)
        : base(correlationId, causationId, occurredAt)
    {
        LinkId = linkId;
        NewExpiresAt = newExpiresAt;
    }
}

