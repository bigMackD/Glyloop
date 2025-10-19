using Glyloop.Domain.Aggregates.Event;
using Glyloop.Domain.Enums;
using Glyloop.Domain.ValueObjects;

namespace Glyloop.Domain.Repositories;

/// <summary>
/// Repository interface for Event aggregate.
/// Events are immutable, so only Add operation is supported (no Update).
/// Reference: DDD Plan Section 2 - Aggregates (Event), Section 6 - Queries
/// </summary>
public interface IEventRepository
{
    /// <summary>
    /// Retrieves an event by its unique identifier.
    /// </summary>
    Task<Event?> GetByIdAsync(Guid eventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves events for a specific user with optional filtering.
    /// </summary>
    /// <param name="userId">User whose events to retrieve</param>
    /// <param name="eventType">Optional filter by event type</param>
    /// <param name="from">Optional start date filter (inclusive)</param>
    /// <param name="to">Optional end date filter (inclusive)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of events matching the criteria, ordered by EventTime descending</returns>
    Task<IReadOnlyList<Event>> GetByUserIdAsync(
        UserId userId,
        EventType? eventType = null,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves events for a specific user within a date range, paginated.
    /// Used for efficient loading of large event histories.
    /// </summary>
    /// <param name="userId">User whose events to retrieve</param>
    /// <param name="from">Start date (inclusive)</param>
    /// <param name="to">End date (inclusive)</param>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Number of events per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of events ordered by EventTime descending</returns>
    Task<IReadOnlyList<Event>> GetPagedAsync(
        UserId userId,
        DateTimeOffset from,
        DateTimeOffset to,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts events for a user within a date range.
    /// Used for pagination calculations.
    /// </summary>
    Task<int> CountByUserIdAsync(
        UserId userId,
        DateTimeOffset from,
        DateTimeOffset to,
        EventType? eventType = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new event to the repository.
    /// Note: Events are immutable - no Update method is provided.
    /// </summary>
    void Add(Event @event);

    /// <summary>
    /// Removes an event from the repository.
    /// In production, consider soft-delete or marking as deleted
    /// while preserving audit trail.
    /// </summary>
    void Remove(Event @event);
}

