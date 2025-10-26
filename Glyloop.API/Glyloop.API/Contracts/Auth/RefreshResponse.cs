namespace Glyloop.API.Contracts.Auth;

/// <summary>
/// Response after successful token refresh.
/// New JWT is set in httpOnly cookie.
/// </summary>
public record RefreshResponse(
    Guid UserId,
    string Email,
    DateTimeOffset RefreshedAt);

