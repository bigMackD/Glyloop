using Glyloop.Domain.Aggregates.DexcomLink;
using Glyloop.Domain.Repositories;
using Glyloop.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Glyloop.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for DexcomLink aggregate.
/// Handles persistence and retrieval of Dexcom OAuth links.
/// </summary>
public class DexcomLinkRepository : IDexcomLinkRepository
{
    private readonly GlyloopDbContext _context;

    public DexcomLinkRepository(GlyloopDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc/>
    public async Task<DexcomLink?> GetByIdAsync(Guid linkId, CancellationToken cancellationToken = default)
    {
        return await _context.DexcomLinks
            .FindAsync(new object[] { linkId }, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<DexcomLink>> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        return await _context.DexcomLinks
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.LastRefreshedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<DexcomLink?> GetActiveByUserIdAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        
        return await _context.DexcomLinks
            .Where(l => l.UserId == userId && l.TokenExpiresAt > now)
            .OrderByDescending(l => l.TokenExpiresAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<DexcomLink>> GetLinksNeedingRefreshAsync(CancellationToken cancellationToken = default)
    {
        // Get links expiring within the next hour (proactive refresh threshold)
        var refreshThreshold = DateTimeOffset.UtcNow.AddHours(1);
        
        return await _context.DexcomLinks
            .Where(l => l.TokenExpiresAt < refreshThreshold)
            .OrderBy(l => l.TokenExpiresAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public void Add(DexcomLink link)
    {
        _context.DexcomLinks.Add(link);
    }

    /// <inheritdoc/>
    public void Remove(DexcomLink link)
    {
        _context.DexcomLinks.Remove(link);
    }
}

