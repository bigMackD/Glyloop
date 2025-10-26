using System.ComponentModel.DataAnnotations;

namespace Glyloop.API.Contracts.Auth;

/// <summary>
/// Request to authenticate a user with credentials.
/// </summary>
public record LoginRequest
{
    /// <summary>
    /// User's email address.
    /// </summary>
    [Required]
    [EmailAddress]
    public required string Email { get; init; }

    /// <summary>
    /// User's password.
    /// </summary>
    [Required]
    public required string Password { get; init; }
}

