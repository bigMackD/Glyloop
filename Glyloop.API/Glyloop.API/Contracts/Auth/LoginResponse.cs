namespace Glyloop.API.Contracts.Auth;

/// <summary>
/// Response after successful login.
/// JWT tokens are set in httpOnly cookies and not included in response body.
/// </summary>
public record LoginResponse(
    Guid UserId,
    string Email);

