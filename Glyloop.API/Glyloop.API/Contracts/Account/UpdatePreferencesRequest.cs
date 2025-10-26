using System.ComponentModel.DataAnnotations;

namespace Glyloop.API.Contracts.Account;

/// <summary>
/// Request to update user's Time in Range preferences.
/// </summary>
public record UpdatePreferencesRequest
{
    /// <summary>
    /// Lower bound of target glucose range in mg/dL (0-1000).
    /// </summary>
    [Required]
    [Range(0, 1000, ErrorMessage = "TIR lower bound must be between 0 and 1000 mg/dL")]
    public required int TirLowerBound { get; init; }

    /// <summary>
    /// Upper bound of target glucose range in mg/dL (0-1000).
    /// Must be greater than lower bound.
    /// </summary>
    [Required]
    [Range(0, 1000, ErrorMessage = "TIR upper bound must be between 0 and 1000 mg/dL")]
    public required int TirUpperBound { get; init; }
}

