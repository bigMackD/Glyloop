using Glyloop.Domain.Aggregates.DexcomLink;
using Glyloop.Domain.ValueObjects;

namespace Glyloop.Domain.Repositories;

/// <summary>
/// Repository interface for DexcomLink aggregate.
/// Only aggregate roots have repositories.
/// Reference: DDD Plan Section 2 - Aggregates (DexcomLink)
/// </summary>
public interface IDexcomLinkRepository
{
    /// <summary>
    /// Retrieves a Dexcom link by its unique identifier.
    /// </summary>
    Task<DexcomLink?> GetByIdAsync(Guid linkId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all active Dexcom links for a specific user.
    /// Business rule: Only one active link per user should exist, but this returns a collection
    /// to support migration scenarios or historical data.
    /// </summary>
    Task<IReadOnlyList<DexcomLink>> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the active Dexcom link for a user, if one exists.
    /// </summary>
    Task<DexcomLink?> GetActiveByUserIdAsync(UserId userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all links that need token refresh (expiring within threshold).
    /// For MVP: used by manual refresh endpoint.
    /// Future: used by background scheduler for automatic refresh.
    /// </summary>
    Task<IReadOnlyList<DexcomLink>> GetLinksNeedingRefreshAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new Dexcom link to the repository.
    /// </summary>
    void Add(DexcomLink link);

    /// <summary>
    /// Removes a Dexcom link from the repository.
    /// </summary>
    void Remove(DexcomLink link);
}

