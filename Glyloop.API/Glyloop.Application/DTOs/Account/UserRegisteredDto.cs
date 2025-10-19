namespace Glyloop.Application.DTOs.Account;

/// <summary>
/// Response DTO containing registered user information.
/// </summary>
public record UserRegisteredDto(
    Guid UserId,
    string Email,
    DateTimeOffset CreatedAt);

