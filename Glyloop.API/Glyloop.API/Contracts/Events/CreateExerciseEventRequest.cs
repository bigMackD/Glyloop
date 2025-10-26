using System.ComponentModel.DataAnnotations;

namespace Glyloop.API.Contracts.Events;

/// <summary>
/// Request to log an exercise event.
/// </summary>
public record CreateExerciseEventRequest
{
    /// <summary>
    /// Time when exercise started. Cannot be in the future.
    /// </summary>
    [Required]
    public required DateTimeOffset EventTime { get; init; }

    /// <summary>
    /// Type/ID of exercise activity.
    /// </summary>
    [Required]
    public required int ExerciseTypeId { get; init; }

    /// <summary>
    /// Duration of exercise in minutes (1-300).
    /// </summary>
    [Required]
    [Range(1, 300, ErrorMessage = "Duration must be between 1 and 300 minutes")]
    public required int DurationMinutes { get; init; }

    /// <summary>
    /// Intensity level (Low, Moderate, High).
    /// </summary>
    public string? Intensity { get; init; }

    /// <summary>
    /// Optional note (max 500 characters).
    /// </summary>
    [MaxLength(500, ErrorMessage = "Note cannot exceed 500 characters")]
    public string? Note { get; init; }
}

