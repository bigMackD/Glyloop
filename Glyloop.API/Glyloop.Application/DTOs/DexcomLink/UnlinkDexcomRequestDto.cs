namespace Glyloop.Application.DTOs.DexcomLink;

/// <summary>
/// Request DTO for unlinking Dexcom account.
/// </summary>
public record UnlinkDexcomRequestDto(
    bool PurgeData = false);

