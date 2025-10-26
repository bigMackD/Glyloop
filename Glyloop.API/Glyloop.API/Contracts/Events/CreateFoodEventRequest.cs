using System.ComponentModel.DataAnnotations;

namespace Glyloop.API.Contracts.Events;

/// <summary>
/// Request to log a food intake event.
/// </summary>
public record CreateFoodEventRequest
{
    /// <summary>
    /// Time when food was consumed. Cannot be in the future.
    /// </summary>
    [Required]
    public required DateTimeOffset EventTime { get; init; }

    /// <summary>
    /// Amount of carbohydrates in grams (0-300).
    /// </summary>
    [Required]
    [Range(0, 300, ErrorMessage = "Carbohydrates must be between 0 and 300 grams")]
    public required int CarbohydratesGrams { get; init; }

    /// <summary>
    /// Meal tag ID for categorization (e.g., breakfast, lunch, dinner).
    /// </summary>
    public int? MealTagId { get; init; }

    /// <summary>
    /// Absorption hint (Fast, Medium, Slow).
    /// </summary>
    public string? AbsorptionHint { get; init; }

    /// <summary>
    /// Optional note (max 500 characters).
    /// </summary>
    [MaxLength(500, ErrorMessage = "Note cannot exceed 500 characters")]
    public string? Note { get; init; }
}

