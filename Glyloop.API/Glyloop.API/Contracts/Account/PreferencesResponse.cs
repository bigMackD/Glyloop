namespace Glyloop.API.Contracts.Account;

/// <summary>
/// User's Time in Range (TIR) preferences.
/// </summary>
public record PreferencesResponse(
    int TirLowerBound,
    int TirUpperBound);

