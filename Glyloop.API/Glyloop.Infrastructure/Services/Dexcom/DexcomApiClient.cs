using Glyloop.Domain.Common;
using Glyloop.Infrastructure.Services.Dexcom.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Glyloop.Infrastructure.Services.Dexcom;

/// <summary>
/// HTTP client for Dexcom API integration.
/// Handles OAuth flow and CGM data retrieval with resilience patterns.
/// 
/// Resilience:
/// - Polly retry policy for transient failures (configured in DI)
/// - Circuit breaker for cascading failure prevention (configured in DI)
/// - Rate limit (429) handling with exponential backoff
/// 
/// Configuration required:
/// - Dexcom:BaseUrl (sandbox or production)
/// - Dexcom:ClientId
/// - Dexcom:ClientSecret
/// - Dexcom:RedirectUri
/// </summary>
public class DexcomApiClient : IDexcomApiClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DexcomApiClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public DexcomApiClient(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<DexcomApiClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Configure JSON serialization options (camelCase property names)
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    /// <inheritdoc/>
    public async Task<Result<OAuthTokenResponse>> ExchangeCodeForTokensAsync(
        string authorizationCode,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(authorizationCode))
            return Result.Failure<OAuthTokenResponse>(
                Error.Create("Dexcom.InvalidCode", "Authorization code cannot be empty."));

        try
        {
            var clientId = _configuration["Dexcom:ClientId"];
            var clientSecret = _configuration["Dexcom:ClientSecret"];
            var redirectUri = _configuration["Dexcom:RedirectUri"];

            var requestBody = new Dictionary<string, string>
            {
                ["client_id"] = clientId!,
                ["client_secret"] = clientSecret!,
                ["code"] = authorizationCode,
                ["grant_type"] = "authorization_code",
                ["redirect_uri"] = redirectUri!
            };

            var response = await _httpClient.PostAsync(
                "/v2/oauth2/token",
                new FormUrlEncodedContent(requestBody),
                cancellationToken);

            return await HandleOAuthResponseAsync(response, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error during code exchange");
            return Result.Failure<OAuthTokenResponse>(
                Error.Create("Dexcom.NetworkError", "Failed to connect to Dexcom API."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during code exchange");
            return Result.Failure<OAuthTokenResponse>(
                Error.Create("Dexcom.UnexpectedError", "An unexpected error occurred."));
        }
    }

    /// <inheritdoc/>
    public async Task<Result<OAuthTokenResponse>> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return Result.Failure<OAuthTokenResponse>(
                Error.Create("Dexcom.InvalidToken", "Refresh token cannot be empty."));

        try
        {
            var clientId = _configuration["Dexcom:ClientId"];
            var clientSecret = _configuration["Dexcom:ClientSecret"];

            var requestBody = new Dictionary<string, string>
            {
                ["client_id"] = clientId!,
                ["client_secret"] = clientSecret!,
                ["refresh_token"] = refreshToken,
                ["grant_type"] = "refresh_token"
            };

            var response = await _httpClient.PostAsync(
                "/v2/oauth2/token",
                new FormUrlEncodedContent(requestBody),
                cancellationToken);

            return await HandleOAuthResponseAsync(response, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error during token refresh");
            return Result.Failure<OAuthTokenResponse>(
                Error.Create("Dexcom.NetworkError", "Failed to connect to Dexcom API."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during token refresh");
            return Result.Failure<OAuthTokenResponse>(
                Error.Create("Dexcom.UnexpectedError", "An unexpected error occurred."));
        }
    }

    /// <inheritdoc/>
    public async Task<Result<GlucoseReadingsResponse>> GetGlucoseReadingsAsync(
        string accessToken,
        DateTimeOffset startTime,
        DateTimeOffset endTime,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
            return Result.Failure<GlucoseReadingsResponse>(
                Error.Create("Dexcom.InvalidToken", "Access token cannot be empty."));

        if (startTime >= endTime)
            return Result.Failure<GlucoseReadingsResponse>(
                Error.Create("Dexcom.InvalidDateRange", "Start time must be before end time."));

        try
        {
            // Format dates in ISO 8601 format for Dexcom API
            var startParam = startTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss");
            var endParam = endTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss");

            var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"/v3/users/self/egvs?startDate={startParam}&endDate={endParam}");

            request.Headers.Add("Authorization", $"Bearer {accessToken}");

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                // Handle 429 rate limiting
                var retryAfter = response.Headers.RetryAfter?.Delta?.TotalSeconds ?? 60;
                _logger.LogWarning("Rate limited by Dexcom API. Retry after {Seconds}s", retryAfter);
                
                return Result.Failure<GlucoseReadingsResponse>(
                    Error.Create(
                        "Dexcom.RateLimited",
                        $"Rate limit exceeded. Retry after {retryAfter} seconds."));
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                _logger.LogWarning("Unauthorized access to Dexcom API. Token may be expired.");
                return Result.Failure<GlucoseReadingsResponse>(
                    Error.Create("Dexcom.Unauthorized", "Access token is invalid or expired."));
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Dexcom API error: {StatusCode} - {Content}", 
                    response.StatusCode, errorContent);
                
                return Result.Failure<GlucoseReadingsResponse>(
                    Error.Create("Dexcom.ApiError", $"API returned status {response.StatusCode}."));
            }

            var readings = await response.Content.ReadFromJsonAsync<GlucoseReadingsResponse>(
                _jsonOptions,
                cancellationToken);

            if (readings == null)
            {
                return Result.Failure<GlucoseReadingsResponse>(
                    Error.Create("Dexcom.InvalidResponse", "Failed to deserialize API response."));
            }

            _logger.LogInformation("Retrieved {Count} glucose readings from Dexcom", 
                readings.Records.Count);

            return Result.Success(readings);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error during glucose reading retrieval");
            return Result.Failure<GlucoseReadingsResponse>(
                Error.Create("Dexcom.NetworkError", "Failed to connect to Dexcom API."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during glucose reading retrieval");
            return Result.Failure<GlucoseReadingsResponse>(
                Error.Create("Dexcom.UnexpectedError", "An unexpected error occurred."));
        }
    }

    /// <summary>
    /// Handles OAuth token response, including error cases.
    /// </summary>
    private async Task<Result<OAuthTokenResponse>> HandleOAuthResponseAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            var retryAfter = response.Headers.RetryAfter?.Delta?.TotalSeconds ?? 60;
            _logger.LogWarning("Rate limited by Dexcom OAuth. Retry after {Seconds}s", retryAfter);
            
            return Result.Failure<OAuthTokenResponse>(
                Error.Create(
                    "Dexcom.RateLimited",
                    $"Rate limit exceeded. Retry after {retryAfter} seconds."));
        }

        if (!response.IsSuccessStatusCode)
        {
            // Try to parse error response
            try
            {
                var errorResponse = await response.Content.ReadFromJsonAsync<DexcomErrorResponse>(
                    _jsonOptions,
                    cancellationToken);

                if (errorResponse != null)
                {
                    _logger.LogError("Dexcom OAuth error: {Error} - {Description}",
                        errorResponse.Error, errorResponse.ErrorDescription);

                    return Result.Failure<OAuthTokenResponse>(
                        Error.Create($"Dexcom.{errorResponse.Error}", errorResponse.ErrorDescription));
                }
            }
            catch
            {
                // If error parsing fails, return generic error
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Dexcom OAuth failed: {StatusCode} - {Content}", 
                response.StatusCode, content);

            return Result.Failure<OAuthTokenResponse>(
                Error.Create("Dexcom.OAuthError", "OAuth request failed."));
        }

        var tokenResponse = await response.Content.ReadFromJsonAsync<OAuthTokenResponse>(
            _jsonOptions,
            cancellationToken);

        if (tokenResponse == null)
        {
            return Result.Failure<OAuthTokenResponse>(
                Error.Create("Dexcom.InvalidResponse", "Failed to deserialize token response."));
        }

        return Result.Success(tokenResponse);
    }
}

