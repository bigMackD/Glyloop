namespace Glyloop.Application.DTOs.DexcomLink;

/// <summary>
/// Response DTO after successfully linking Dexcom account.
/// </summary>
public record DexcomLinkCreatedDto(
    Guid LinkId,
    Guid UserId,
    DateTimeOffset TokenExpiresAt,
    DateTimeOffset CreatedAt);

