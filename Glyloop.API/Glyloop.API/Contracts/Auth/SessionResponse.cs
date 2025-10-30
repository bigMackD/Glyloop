namespace Glyloop.API.Contracts.Auth;

/// <summary>
/// Response containing current user session information.
/// </summary>
public record SessionResponse(
    Guid UserId,
    string Email);


