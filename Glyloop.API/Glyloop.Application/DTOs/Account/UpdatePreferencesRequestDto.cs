namespace Glyloop.Application.DTOs.Account;

/// <summary>
/// Request DTO for updating user preferences (TIR range).
/// </summary>
public record UpdatePreferencesRequestDto(
    int LowerBound,
    int UpperBound);

