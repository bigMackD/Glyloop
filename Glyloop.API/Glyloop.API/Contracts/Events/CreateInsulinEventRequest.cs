using System.ComponentModel.DataAnnotations;

namespace Glyloop.API.Contracts.Events;

/// <summary>
/// Request to log an insulin dose event.
/// </summary>
public record CreateInsulinEventRequest
{
    /// <summary>
    /// Time when insulin was administered. Cannot be in the future.
    /// </summary>
    [Required]
    public required DateTimeOffset EventTime { get; init; }

    /// <summary>
    /// Type of insulin (Fast or Long).
    /// </summary>
    [Required]
    public required string InsulinType { get; init; }

    /// <summary>
    /// Insulin dose in units (0-100, increments of 0.5).
    /// </summary>
    [Required]
    [Range(0, 100, ErrorMessage = "Insulin dose must be between 0 and 100 units")]
    public required decimal InsulinUnits { get; init; }

    /// <summary>
    /// Optional preparation details.
    /// </summary>
    [MaxLength(200)]
    public string? Preparation { get; init; }

    /// <summary>
    /// Optional delivery method.
    /// </summary>
    [MaxLength(200)]
    public string? Delivery { get; init; }

    /// <summary>
    /// Optional timing details.
    /// </summary>
    [MaxLength(200)]
    public string? Timing { get; init; }

    /// <summary>
    /// Optional note (max 500 characters).
    /// </summary>
    [MaxLength(500, ErrorMessage = "Note cannot exceed 500 characters")]
    public string? Note { get; init; }
}

