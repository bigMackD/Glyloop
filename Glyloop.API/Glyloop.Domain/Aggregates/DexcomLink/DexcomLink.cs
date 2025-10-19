using Glyloop.Domain.Aggregates.DexcomLink.Events;
using Glyloop.Domain.Common;
using Glyloop.Domain.Errors;
using Glyloop.Domain.ValueObjects;

namespace Glyloop.Domain.Aggregates.DexcomLink;

/// <summary>
/// Aggregate root representing a link between a user and their Dexcom account.
/// Manages OAuth token lifecycle and refresh operations.
/// 
/// Invariants:
/// - TokenExpiresAt must be greater than the current time (for active links)
/// - Refresh operations must follow OAuth token rotation policy
/// - Only one active link per user (enforced at repository level)
/// 
/// Reference: DDD Plan Section 2 - Aggregates (DexcomLink)
/// 
/// Infrastructure mapping notes:
/// - EncryptedAccessToken and EncryptedRefreshToken should be stored as binary columns
/// - Timestamps should be stored with UTC offset
/// - Consider indexing on UserId for retrieval by user
/// </summary>
public sealed class DexcomLink : AggregateRoot<Guid>
{
    /// <summary>
    /// Gets the user who owns this Dexcom link.
    /// </summary>
    public UserId UserId { get; private set; }

    /// <summary>
    /// Gets the encrypted OAuth access token.
    /// Encryption is handled by Infrastructure layer.
    /// </summary>
    public byte[] EncryptedAccessToken { get; private set; }

    /// <summary>
    /// Gets the encrypted OAuth refresh token.
    /// Encryption is handled by Infrastructure layer.
    /// </summary>
    public byte[] EncryptedRefreshToken { get; private set; }

    /// <summary>
    /// Gets the expiration time of the access token (UTC).
    /// </summary>
    public DateTimeOffset TokenExpiresAt { get; private set; }

    /// <summary>
    /// Gets the last time the tokens were refreshed (UTC).
    /// </summary>
    public DateTimeOffset LastRefreshedAt { get; private set; }

    /// <summary>
    /// Gets whether the link is currently active (not expired).
    /// </summary>
    public bool IsActive => DateTimeOffset.UtcNow < TokenExpiresAt;

    /// <summary>
    /// Gets whether the token should be refreshed proactively.
    /// Returns true if token expires within 1 hour.
    /// For MVP: used for manual refresh decision.
    /// Future: used by background worker for automatic refresh scheduling.
    /// </summary>
    public bool ShouldRefresh => DateTimeOffset.UtcNow.AddHours(1) >= TokenExpiresAt;

    // EF Core constructor
    private DexcomLink() : base(Guid.Empty)
    {
        UserId = null!;
        EncryptedAccessToken = null!;
        EncryptedRefreshToken = null!;
    }

    private DexcomLink(
        Guid id,
        UserId userId,
        byte[] encryptedAccessToken,
        byte[] encryptedRefreshToken,
        DateTimeOffset tokenExpiresAt,
        DateTimeOffset lastRefreshedAt) : base(id)
    {
        UserId = userId;
        EncryptedAccessToken = encryptedAccessToken;
        EncryptedRefreshToken = encryptedRefreshToken;
        TokenExpiresAt = tokenExpiresAt;
        LastRefreshedAt = lastRefreshedAt;
    }

    /// <summary>
    /// Creates a new Dexcom link after successful OAuth authorization.
    /// </summary>
    /// <param name="userId">The user linking their account</param>
    /// <param name="encryptedAccessToken">Encrypted OAuth access token</param>
    /// <param name="encryptedRefreshToken">Encrypted OAuth refresh token</param>
    /// <param name="tokenExpiresAt">When the access token expires</param>
    /// <param name="timeProvider">Time provider for timestamp</param>
    /// <param name="correlationId">Correlation ID for event tracking</param>
    /// <param name="causationId">Causation ID for event tracking</param>
    /// <returns>New DexcomLink aggregate</returns>
    public static Result<DexcomLink> Create(
        UserId userId,
        byte[] encryptedAccessToken,
        byte[] encryptedRefreshToken,
        DateTimeOffset tokenExpiresAt,
        ITimeProvider timeProvider,
        Guid correlationId,
        Guid causationId)
    {
        // Validate inputs
        if (encryptedAccessToken == null || encryptedAccessToken.Length == 0)
            return Result.Failure<DexcomLink>(
                Error.Create("DexcomLink.InvalidToken", "Encrypted access token cannot be empty."));

        if (encryptedRefreshToken == null || encryptedRefreshToken.Length == 0)
            return Result.Failure<DexcomLink>(
                Error.Create("DexcomLink.InvalidToken", "Encrypted refresh token cannot be empty."));

        var now = timeProvider.UtcNow;
        if (tokenExpiresAt <= now)
            return Result.Failure<DexcomLink>(DomainErrors.DexcomLink.TokenExpired);

        var linkId = Guid.NewGuid();
        var link = new DexcomLink(
            linkId,
            userId,
            encryptedAccessToken,
            encryptedRefreshToken,
            tokenExpiresAt,
            now);

        // Raise domain event
        link.RaiseDomainEvent(new DexcomLinkedEvent(
            userId,
            linkId,
            correlationId,
            causationId,
            now));

        return Result.Success(link);
    }

    /// <summary>
    /// Refreshes the OAuth tokens using the refresh token.
    /// Implements token rotation - both access and refresh tokens are updated.
    /// </summary>
    /// <param name="newEncryptedAccessToken">New encrypted access token</param>
    /// <param name="newEncryptedRefreshToken">New encrypted refresh token (rotated)</param>
    /// <param name="newTokenExpiresAt">New expiration time</param>
    /// <param name="timeProvider">Time provider</param>
    /// <param name="correlationId">Correlation ID for event tracking</param>
    /// <param name="causationId">Causation ID for event tracking</param>
    public Result RefreshTokens(
        byte[] newEncryptedAccessToken,
        byte[] newEncryptedRefreshToken,
        DateTimeOffset newTokenExpiresAt,
        ITimeProvider timeProvider,
        Guid correlationId,
        Guid causationId)
    {
        // Validate inputs
        if (newEncryptedAccessToken == null || newEncryptedAccessToken.Length == 0)
            return Result.Failure(
                Error.Create("DexcomLink.InvalidToken", "Encrypted access token cannot be empty."));

        if (newEncryptedRefreshToken == null || newEncryptedRefreshToken.Length == 0)
            return Result.Failure(
                Error.Create("DexcomLink.InvalidToken", "Encrypted refresh token cannot be empty."));

        var now = timeProvider.UtcNow;
        if (newTokenExpiresAt <= now)
            return Result.Failure(DomainErrors.DexcomLink.TokenExpired);

        // Update tokens
        EncryptedAccessToken = newEncryptedAccessToken;
        EncryptedRefreshToken = newEncryptedRefreshToken;
        TokenExpiresAt = newTokenExpiresAt;
        LastRefreshedAt = now;

        // Raise domain event
        RaiseDomainEvent(new DexcomTokensRefreshedEvent(
            Id,
            newTokenExpiresAt,
            correlationId,
            causationId,
            now));

        return Result.Success();
    }

    /// <summary>
    /// Marks this link as unlinked (soft delete or hard delete handled by repository).
    /// </summary>
    /// <param name="purgeData">Whether associated data should be purged</param>
    /// <param name="correlationId">Correlation ID for event tracking</param>
    /// <param name="causationId">Causation ID for event tracking</param>
    public void Unlink(bool purgeData, Guid correlationId, Guid causationId)
    {
        RaiseDomainEvent(new DexcomUnlinkedEvent(
            UserId,
            Id,
            purgeData,
            correlationId,
            causationId));
    }
}

