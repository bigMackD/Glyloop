namespace Glyloop.API.Contracts.Dexcom;

/// <summary>
/// Response after successfully linking Dexcom account.
/// </summary>
public record LinkDexcomResponse(
    Guid LinkId,
    DateTimeOffset LinkedAt,
    DateTimeOffset TokenExpiresAt);

