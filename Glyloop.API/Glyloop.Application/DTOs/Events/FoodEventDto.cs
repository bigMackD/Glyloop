using Glyloop.Domain.Enums;

namespace Glyloop.Application.DTOs.Events;

/// <summary>
/// DTO for food intake events.
/// </summary>
public record FoodEventDto(
    Guid EventId,
    Guid UserId,
    EventType EventType,
    DateTimeOffset EventTime,
    DateTimeOffset CreatedAt,
    string? Note,
    int CarbohydratesGrams,
    int MealTagId,
    AbsorptionHint AbsorptionHint) : EventDto(EventId, UserId, EventType, EventTime, CreatedAt, Note);

