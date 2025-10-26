namespace Glyloop.API.Contracts.Dexcom;

/// <summary>
/// Current status of Dexcom integration.
/// </summary>
public record DexcomStatusResponse(
    bool IsLinked,
    DateTimeOffset? LinkedAt,
    DateTimeOffset? TokenExpiresAt,
    DateTimeOffset? LastSyncAt);

