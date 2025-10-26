namespace Glyloop.API.Contracts.Events;

/// <summary>
/// Summary response for event in list views.
/// </summary>
public record EventListItemResponse(
    Guid EventId,
    string EventType,
    DateTimeOffset EventTime,
    string Summary);

