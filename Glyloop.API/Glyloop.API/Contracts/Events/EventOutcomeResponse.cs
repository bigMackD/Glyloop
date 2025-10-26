namespace Glyloop.API.Contracts.Events;

/// <summary>
/// Response for food event +2h outcome.
/// </summary>
public record EventOutcomeResponse(
    Guid EventId,
    DateTimeOffset EventTime,
    DateTimeOffset OutcomeTime,
    int? GlucoseValue,
    bool IsApproximate,
    string? Message);

