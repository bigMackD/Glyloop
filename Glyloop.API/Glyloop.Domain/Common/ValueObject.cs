namespace Glyloop.Domain.Common;

/// <summary>
/// Base class for all value objects in the domain.
/// Value objects are defined by their attributes, not their identity.
/// They are immutable and two value objects with the same attributes are considered equal.
/// </summary>
public abstract class ValueObject : IEquatable<ValueObject>
{
    /// <summary>
    /// Gets the atomic values that make up this value object for equality comparison.
    /// </summary>
    protected abstract IEnumerable<object?> GetEqualityComponents();

    public bool Equals(ValueObject? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;

        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    public override bool Equals(object? obj)
    {
        return obj is ValueObject valueObject && Equals(valueObject);
    }

    public override int GetHashCode()
    {
        return GetEqualityComponents()
            .Aggregate(default(int), (current, obj) =>
            {
                unchecked
                {
                    return (current * 397) ^ (obj?.GetHashCode() ?? 0);
                }
            });
    }

    public static bool operator ==(ValueObject? left, ValueObject? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(ValueObject? left, ValueObject? right)
    {
        return !Equals(left, right);
    }
}

