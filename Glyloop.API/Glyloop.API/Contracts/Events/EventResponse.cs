namespace Glyloop.API.Contracts.Events;

/// <summary>
/// Base response for event data.
/// </summary>
public record EventResponse(
    Guid EventId,
    string EventType,
    DateTimeOffset EventTime,
    DateTimeOffset CreatedAt,
    string? Note);

/// <summary>
/// Response for food event with specific details.
/// </summary>
public record FoodEventResponse(
    Guid EventId,
    string EventType,
    DateTimeOffset EventTime,
    DateTimeOffset CreatedAt,
    string? Note,
    int CarbohydratesGrams,
    int? MealTagId,
    string? AbsorptionHint) : EventResponse(EventId, EventType, EventTime, CreatedAt, Note);

/// <summary>
/// Response for insulin event with specific details.
/// </summary>
public record InsulinEventResponse(
    Guid EventId,
    string EventType,
    DateTimeOffset EventTime,
    DateTimeOffset CreatedAt,
    string? Note,
    string InsulinType,
    decimal InsulinUnits,
    string? Preparation,
    string? Delivery,
    string? Timing) : EventResponse(EventId, EventType, EventTime, CreatedAt, Note);

/// <summary>
/// Response for exercise event with specific details.
/// </summary>
public record ExerciseEventResponse(
    Guid EventId,
    string EventType,
    DateTimeOffset EventTime,
    DateTimeOffset CreatedAt,
    string? Note,
    int ExerciseTypeId,
    int DurationMinutes,
    string? Intensity) : EventResponse(EventId, EventType, EventTime, CreatedAt, Note);

/// <summary>
/// Response for note event.
/// </summary>
public record NoteEventResponse(
    Guid EventId,
    string EventType,
    DateTimeOffset EventTime,
    DateTimeOffset CreatedAt,
    string NoteText) : EventResponse(EventId, EventType, EventTime, CreatedAt, NoteText);

