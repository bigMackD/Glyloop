using Glyloop.Application.Common.Interfaces;
using Glyloop.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Glyloop.Infrastructure.Persistence;

/// <summary>
/// Unit of Work pattern implementation.
/// Coordinates database transaction and domain event dispatching.
/// 
/// Process:
/// 1. Collects domain events from tracked aggregates
/// 2. Clears events from aggregates
/// 3. Persists changes to database
/// 4. Dispatches domain events via MediatR (eventual consistency)
/// 
/// Design decision: Domain events are dispatched AFTER successful persistence.
/// This ensures database consistency but means event handlers can't prevent the save.
/// For transactional consistency, wrap in IDbContextTransaction if needed.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly GlyloopDbContext _context;
    private readonly IMediator _mediator;

    public UnitOfWork(GlyloopDbContext context, IMediator mediator)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    /// <inheritdoc/>
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default)
    {
        // 1. Get all tracked aggregates that have domain events
        var aggregatesWithEvents = _context.ChangeTracker
            .Entries<Entity<Guid>>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();

        // 2. Collect all domain events before saving
        var domainEvents = aggregatesWithEvents
            .SelectMany(a => a.DomainEvents)
            .ToList();

        // 3. Clear domain events from aggregates
        // This prevents re-dispatching if SaveChanges is called multiple times
        foreach (var aggregate in aggregatesWithEvents)
        {
            aggregate.ClearDomainEvents();
        }

        // 4. Save changes to database
        // If this fails, domain events won't be dispatched
        await _context.SaveChangesAsync(cancellationToken);

        // 5. Dispatch domain events via MediatR
        // Events are published after successful persistence (eventual consistency)
        foreach (var domainEvent in domainEvents)
        {
            await _mediator.Publish(domainEvent, cancellationToken);
        }

        return true;
    }
}

