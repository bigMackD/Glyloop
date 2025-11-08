using Glyloop.Domain.Aggregates.Event;
using Glyloop.Domain.Enums;
using Glyloop.Domain.Repositories;
using Glyloop.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Glyloop.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for Event aggregate.
/// Handles persistence and retrieval of all event types (Food, Insulin, Exercise, Note).
/// EF Core TPT inheritance handles polymorphic loading automatically.
/// </summary>
public class EventRepository : IEventRepository
{
    private readonly GlyloopDbContext _context;

    public EventRepository(GlyloopDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc/>
    public async Task<Event?> GetByIdAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        // EF Core automatically loads the correct derived type with TPT
        return await _context.Events
            .FindAsync(new object[] { eventId }, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Event>> GetByUserIdAsync(
        UserId userId,
        EventType? eventType = null,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Events.AsQueryable();

        // Filter by user
        query = query.Where(e => e.UserId == userId);

        // Optional filter by event type
        if (eventType.HasValue)
        {
            query = query.Where(e => e.EventType == eventType.Value);
        }

        // Optional filter by date range
        if (from.HasValue)
        {
            query = query.Where(e => e.EventTime >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(e => e.EventTime <= to.Value);
        }

        // Order by event time descending (most recent first)
        return await query
            .OrderByDescending(e => e.EventTime)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Event>> GetPagedAsync(
        UserId userId,
        DateTimeOffset from,
        DateTimeOffset to,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        // Ensure page number is at least 1
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100); // Limit max page size to 100

        return await _context.Events
            .Where(e => e.UserId == userId
                && e.EventTime >= from
                && e.EventTime <= to)
            .OrderByDescending(e => e.EventTime)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<int> CountByUserIdAsync(
        UserId userId,
        DateTimeOffset from,
        DateTimeOffset to,
        EventType? eventType = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Events
            .Where(e => e.UserId == userId
                && e.EventTime >= from
                && e.EventTime <= to);

        if (eventType.HasValue)
        {
            query = query.Where(e => e.EventType == eventType.Value);
        }

        return await query.CountAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public void Add(Event @event)
    {
        _context.Events.Add(@event);
    }

    /// <inheritdoc/>
    public void Remove(Event @event)
    {
        _context.Events.Remove(@event);
    }
}

