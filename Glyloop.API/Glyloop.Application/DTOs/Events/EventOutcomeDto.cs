namespace Glyloop.Application.DTOs.Events;

/// <summary>
/// DTO containing glucose outcome information for a food event.
/// Shows glucose reading approximately 2 hours after food intake.
/// </summary>
public record EventOutcomeDto(
    Guid EventId,
    DateTimeOffset TargetTime,
    int? GlucoseValueMgDl,
    DateTimeOffset? ReadingTime,
    bool HasReading);

