namespace Glyloop.Application.DTOs.DexcomLink;

/// <summary>
/// Response DTO containing Dexcom link status information.
/// </summary>
public record DexcomLinkStatusDto(
    bool IsLinked,
    Guid? LinkId,
    DateTimeOffset? TokenExpiresAt,
    DateTimeOffset? LastRefreshedAt,
    bool? ShouldRefresh);

