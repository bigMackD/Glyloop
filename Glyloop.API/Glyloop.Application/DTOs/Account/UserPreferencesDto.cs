namespace Glyloop.Application.DTOs.Account;

/// <summary>
/// Response DTO containing user preferences.
/// </summary>
public record UserPreferencesDto(
    Guid UserId,
    int TirLowerBound,
    int TirUpperBound);

