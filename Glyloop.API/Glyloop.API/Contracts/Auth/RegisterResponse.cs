namespace Glyloop.API.Contracts.Auth;

/// <summary>
/// Response after successful user registration.
/// </summary>
public record RegisterResponse(
    Guid UserId,
    string Email,
    DateTimeOffset RegisteredAt);

