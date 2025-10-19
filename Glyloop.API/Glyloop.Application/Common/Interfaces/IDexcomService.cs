using Glyloop.Domain.Common;

namespace Glyloop.Application.Common.Interfaces;

/// <summary>
/// Service for interacting with Dexcom API.
/// Abstracts Infrastructure implementation from Application layer.
/// </summary>
public interface IDexcomService
{
    /// <summary>
    /// Exchanges an OAuth authorization code for access and refresh tokens.
    /// </summary>
    /// <param name="authorizationCode">Authorization code from OAuth callback</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing token information or error</returns>
    Task<Result<DexcomTokens>> ExchangeCodeForTokensAsync(
        string authorizationCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes an expired access token using the refresh token.
    /// </summary>
    /// <param name="refreshToken">Current refresh token</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing new token information or error</returns>
    Task<Result<DexcomTokens>> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// DTO containing Dexcom OAuth token information.
/// </summary>
public record DexcomTokens(
    string AccessToken,
    string RefreshToken,
    int ExpiresInSeconds);

