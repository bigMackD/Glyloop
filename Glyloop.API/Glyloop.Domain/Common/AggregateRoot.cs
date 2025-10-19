namespace Glyloop.Domain.Common;

/// <summary>
/// Base class for aggregate roots in the domain.
/// Aggregate roots are the only entities that can be retrieved directly from a repository.
/// They enforce consistency boundaries and transactional integrity for their child entities.
/// </summary>
/// <typeparam name="TId">The type of the aggregate root's identifier</typeparam>
public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : notnull
{
    protected AggregateRoot(TId id) : base(id)
    {
    }
}

