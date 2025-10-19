namespace Glyloop.Application.Common.Interfaces;

/// <summary>
/// Unit of Work interface for coordinating transactions and domain events.
/// Implemented by Infrastructure layer to manage database transactions.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Saves all pending changes to the database without dispatching domain events.
    /// Use this for simple operations that don't require event handling.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves all pending changes and dispatches domain events.
    /// This is the preferred method for domain operations that raise events.
    /// </summary>
    /// <returns>True if save was successful, false otherwise</returns>
    Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default);
}

