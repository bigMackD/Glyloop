using Glyloop.Domain.Common;
using Glyloop.Infrastructure.Services.Dexcom.Models;

namespace Glyloop.Infrastructure.Services.Dexcom;

/// <summary>
/// Interface for Dexcom API client.
/// Handles OAuth token exchange, token refresh, and CGM data retrieval.
/// 
/// API Documentation: https://developer.dexcom.com/
/// Sandbox Base URL: https://sandbox-api.dexcom.com
/// Production Base URL: https://api.dexcom.com
/// </summary>
public interface IDexcomApiClient
{
    /// <summary>
    /// Exchanges an OAuth authorization code for access and refresh tokens.
    /// Called after user completes OAuth authorization flow.
    /// </summary>
    /// <param name="authorizationCode">Authorization code from OAuth callback</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing OAuth token response or error</returns>
    Task<Result<OAuthTokenResponse>> ExchangeCodeForTokensAsync(
        string authorizationCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes an expired access token using the refresh token.
    /// Implements token rotation - both access and refresh tokens are updated.
    /// </summary>
    /// <param name="refreshToken">Current refresh token</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing new OAuth token response or error</returns>
    Task<Result<OAuthTokenResponse>> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves glucose readings (EGVs) for a user within a time range.
    /// Maximum range: 90 days. Readings are typically every 5 minutes.
    /// </summary>
    /// <param name="accessToken">Valid OAuth access token</param>
    /// <param name="startTime">Start of time range (UTC)</param>
    /// <param name="endTime">End of time range (UTC)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing glucose readings or error</returns>
    Task<Result<GlucoseReadingsResponse>> GetGlucoseReadingsAsync(
        string accessToken,
        DateTimeOffset startTime,
        DateTimeOffset endTime,
        CancellationToken cancellationToken = default);
}

