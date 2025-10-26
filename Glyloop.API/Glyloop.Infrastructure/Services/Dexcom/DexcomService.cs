using Glyloop.Application.Common.Interfaces;
using Glyloop.Domain.Common;
using Microsoft.Extensions.Logging;

namespace Glyloop.Infrastructure.Services.Dexcom;

/// <summary>
/// Service for interacting with Dexcom API.
/// Provides high-level operations for OAuth and token management.
/// </summary>
public class DexcomService : IDexcomService
{
    private readonly IDexcomApiClient _dexcomApiClient;
    private readonly ILogger<DexcomService> _logger;

    public DexcomService(
        IDexcomApiClient dexcomApiClient,
        ILogger<DexcomService> logger)
    {
        _dexcomApiClient = dexcomApiClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<DexcomTokens>> ExchangeCodeForTokensAsync(
        string authorizationCode,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Exchanging authorization code for Dexcom tokens");

            var result = await _dexcomApiClient.ExchangeCodeForTokensAsync(
                authorizationCode,
                cancellationToken);

            if (result.IsFailure)
            {
                _logger.LogError("Failed to exchange authorization code: {Error}", result.Error.Message);
                return Result.Failure<DexcomTokens>(result.Error);
            }

            var response = result.Value;
            var tokens = new DexcomTokens(
                AccessToken: response.AccessToken,
                RefreshToken: response.RefreshToken,
                ExpiresInSeconds: response.ExpiresIn);

            _logger.LogInformation("Successfully exchanged authorization code for tokens");
            return Result.Success(tokens);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while exchanging authorization code");
            return Result.Failure<DexcomTokens>(Error.Create(
                "DexcomService.HttpError",
                $"HTTP error occurred: {ex.Message}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while exchanging authorization code");
            return Result.Failure<DexcomTokens>(Error.Create(
                "DexcomService.UnexpectedError",
                $"Unexpected error occurred: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<DexcomTokens>> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Refreshing Dexcom access token");

            var result = await _dexcomApiClient.RefreshTokenAsync(
                refreshToken,
                cancellationToken);

            if (result.IsFailure)
            {
                _logger.LogError("Failed to refresh token: {Error}", result.Error.Message);
                return Result.Failure<DexcomTokens>(result.Error);
            }

            var response = result.Value;
            var tokens = new DexcomTokens(
                AccessToken: response.AccessToken,
                RefreshToken: response.RefreshToken,
                ExpiresInSeconds: response.ExpiresIn);

            _logger.LogInformation("Successfully refreshed access token");
            return Result.Success(tokens);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while refreshing token");
            return Result.Failure<DexcomTokens>(Error.Create(
                "DexcomService.HttpError",
                $"HTTP error occurred: {ex.Message}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while refreshing token");
            return Result.Failure<DexcomTokens>(Error.Create(
                "DexcomService.UnexpectedError",
                $"Unexpected error occurred: {ex.Message}"));
        }
    }
}
