using Glyloop.Application.Common.Interfaces;
using Glyloop.Domain.Common;
using Glyloop.Domain.Repositories;
using Glyloop.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Glyloop.Infrastructure.Services.Dexcom;

/// <summary>
/// Service for retrieving glucose reading data from Dexcom API.
/// Manages glucose data retrieval and caching.
/// </summary>
public class GlucoseReadingService : IGlucoseReadingService
{
    private readonly IDexcomApiClient _dexcomApiClient;
    private readonly IDexcomLinkRepository _dexcomLinkRepository;
    private readonly ITokenEncryptionService _tokenEncryptionService;
    private readonly ILogger<GlucoseReadingService> _logger;

    public GlucoseReadingService(
        IDexcomApiClient dexcomApiClient,
        IDexcomLinkRepository dexcomLinkRepository,
        ITokenEncryptionService tokenEncryptionService,
        ILogger<GlucoseReadingService> logger)
    {
        _dexcomApiClient = dexcomApiClient;
        _dexcomLinkRepository = dexcomLinkRepository;
        _tokenEncryptionService = tokenEncryptionService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<GlucoseReading?>> GetReadingNearTimeAsync(
        UserId userId,
        DateTimeOffset targetTime,
        int toleranceMinutes = 15,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get the user's active Dexcom link
            var dexcomLink = await _dexcomLinkRepository.GetActiveByUserIdAsync(userId, cancellationToken);
            if (dexcomLink == null)
            {
                _logger.LogWarning("No active Dexcom link found for user {UserId}", userId.Value);
                return Result.Success<GlucoseReading?>(null);
            }

            // Decrypt access token
            var accessToken = _tokenEncryptionService.Decrypt(dexcomLink.EncryptedAccessToken);

            // Get readings from a window around the target time
            var startTime = targetTime.AddMinutes(-toleranceMinutes);
            var endTime = targetTime.AddMinutes(toleranceMinutes);

            var result = await _dexcomApiClient.GetGlucoseReadingsAsync(
                accessToken,
                startTime,
                endTime,
                cancellationToken);

            if (result.IsFailure)
            {
                _logger.LogError("Failed to get glucose readings: {Error}", result.Error.Message);
                return Result.Failure<GlucoseReading?>(result.Error);
            }

            var readings = result.Value.Records;
            if (readings == null || readings.Count == 0)
            {
                _logger.LogInformation("No readings found near {TargetTime} for user {UserId}", targetTime, userId.Value);
                return Result.Success<GlucoseReading?>(null);
            }

            // Find the reading closest to the target time
            var closestReading = readings
                .OrderBy(r => Math.Abs((r.SystemTime - targetTime).TotalMinutes))
                .First();

            var glucoseReading = new GlucoseReading(
                SystemTime: closestReading.SystemTime,
                ValueMgDl: closestReading.Value,
                Trend: closestReading.Trend);

            return Result.Success<GlucoseReading?>(glucoseReading);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting glucose reading near time for user {UserId}", userId.Value);
            return Result.Failure<GlucoseReading?>(Error.Create(
                "GlucoseReadingService.Error",
                $"Failed to get glucose reading: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<GlucoseReading>>> GetReadingsInRangeAsync(
        UserId userId,
        DateTimeOffset startTime,
        DateTimeOffset endTime,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get the user's active Dexcom link
            var dexcomLink = await _dexcomLinkRepository.GetActiveByUserIdAsync(userId, cancellationToken);
            if (dexcomLink == null)
            {
                _logger.LogWarning("No active Dexcom link found for user {UserId}", userId.Value);
                return Result.Success<IReadOnlyList<GlucoseReading>>(Array.Empty<GlucoseReading>());
            }

            // Decrypt access token
            var accessToken = _tokenEncryptionService.Decrypt(dexcomLink.EncryptedAccessToken);

            // Get readings from Dexcom API
            var result = await _dexcomApiClient.GetGlucoseReadingsAsync(
                accessToken,
                startTime,
                endTime,
                cancellationToken);

            if (result.IsFailure)
            {
                _logger.LogError("Failed to get glucose readings: {Error}", result.Error.Message);
                return Result.Failure<IReadOnlyList<GlucoseReading>>(result.Error);
            }

            var readings = result.Value.Records;
            if (readings == null || readings.Count == 0)
            {
                _logger.LogInformation("No readings found between {StartTime} and {EndTime} for user {UserId}",
                    startTime, endTime, userId.Value);
                return Result.Success<IReadOnlyList<GlucoseReading>>(Array.Empty<GlucoseReading>());
            }

            // Map to application DTOs
            var glucoseReadings = readings
                .Select(r => new GlucoseReading(
                    SystemTime: r.SystemTime,
                    ValueMgDl: r.Value,
                    Trend: r.Trend))
                .ToList();

            _logger.LogInformation("Retrieved {Count} readings for user {UserId}", glucoseReadings.Count, userId.Value);
            return Result.Success<IReadOnlyList<GlucoseReading>>(glucoseReadings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting glucose readings in range for user {UserId}", userId.Value);
            return Result.Failure<IReadOnlyList<GlucoseReading>>(Error.Create(
                "GlucoseReadingService.Error",
                $"Failed to get glucose readings: {ex.Message}"));
        }
    }
}
