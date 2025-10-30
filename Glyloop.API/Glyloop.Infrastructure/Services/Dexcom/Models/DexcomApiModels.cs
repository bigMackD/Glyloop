using System.Text.Json.Serialization;

namespace Glyloop.Infrastructure.Services.Dexcom.Models;

/// <summary>
/// Response from Dexcom OAuth token endpoint.
/// Returned when exchanging authorization code or refreshing tokens.
/// </summary>
/// <param name="AccessToken">OAuth access token for API calls</param>
/// <param name="RefreshToken">Refresh token for obtaining new access tokens</param>
/// <param name="ExpiresIn">Token lifetime in seconds (typically 7200 = 2 hours)</param>
/// <param name="TokenType">Token type (typically "Bearer")</param>
public record OAuthTokenResponse(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("refresh_token")] string RefreshToken,
    [property: JsonPropertyName("expires_in")] int ExpiresIn,
    [property: JsonPropertyName("token_type")] string TokenType);

/// <summary>
/// Response from Dexcom glucose readings endpoint.
/// Contains a list of estimated glucose values (EGVs).
/// </summary>
/// <param name="Records">List of glucose readings</param>
public record GlucoseReadingsResponse(
    List<GlucoseReading> Records);

/// <summary>
/// Individual glucose reading from Dexcom CGM.
/// </summary>
/// <param name="SystemTime">UTC timestamp from the device</param>
/// <param name="DisplayTime">Display timestamp in user's timezone</param>
/// <param name="Value">Glucose value in mg/dL</param>
/// <param name="Unit">Unit of measurement (typically "mg/dL")</param>
/// <param name="Trend">Trend arrow direction (e.g., "flat", "fortyFiveUp", "singleUp")</param>
public record GlucoseReading(
    DateTimeOffset SystemTime,
    DateTimeOffset DisplayTime,
    int Value,
    string Unit,
    string Trend);

/// <summary>
/// Dexcom API error response.
/// Returned when API call fails with error details.
/// </summary>
/// <param name="Error">Error code (e.g., "invalid_grant", "invalid_token")</param>
/// <param name="ErrorDescription">Human-readable error description</param>
public record DexcomErrorResponse(
    string Error,
    string ErrorDescription);

