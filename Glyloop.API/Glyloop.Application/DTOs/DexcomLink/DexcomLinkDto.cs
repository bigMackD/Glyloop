namespace Glyloop.Application.DTOs.DexcomLink;

/// <summary>
/// Response DTO containing Dexcom link information.
/// </summary>
public record DexcomLinkDto(
    Guid LinkId,
    Guid UserId,
    DateTimeOffset TokenExpiresAt,
    DateTimeOffset LastRefreshedAt,
    bool IsActive,
    bool ShouldRefresh);

