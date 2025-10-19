namespace Glyloop.Application.DTOs.DexcomLink;

/// <summary>
/// Request DTO for linking Dexcom account via OAuth.
/// </summary>
public record LinkDexcomRequestDto(
    string AuthorizationCode);

