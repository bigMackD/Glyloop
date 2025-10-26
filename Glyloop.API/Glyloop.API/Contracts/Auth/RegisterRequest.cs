using System.ComponentModel.DataAnnotations;

namespace Glyloop.API.Contracts.Auth;

/// <summary>
/// Request to register a new user account.
/// </summary>
public record RegisterRequest
{
    /// <summary>
    /// User's email address. Must be unique and valid format.
    /// </summary>
    [Required]
    [EmailAddress]
    public required string Email { get; init; }

    /// <summary>
    /// User's password. Must be at least 12 characters.
    /// </summary>
    [Required]
    [MinLength(12, ErrorMessage = "Password must be at least 12 characters")]
    public required string Password { get; init; }
}

