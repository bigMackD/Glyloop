using System.ComponentModel.DataAnnotations;

namespace Glyloop.API.Contracts.Dexcom;

/// <summary>
/// Request to link Dexcom account using OAuth authorization code.
/// </summary>
public record LinkDexcomRequest
{
    /// <summary>
    /// OAuth authorization code received from Dexcom callback.
    /// </summary>
    [Required]
    public required string AuthorizationCode { get; init; }
}

