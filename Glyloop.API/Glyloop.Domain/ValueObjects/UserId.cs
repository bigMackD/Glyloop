using Glyloop.Domain.Common;

namespace Glyloop.Domain.ValueObjects;

/// <summary>
/// Represents a user identifier that links domain entities to an authenticated user.
/// This is a lightweight reference to ASP.NET Core Identity's user without directly coupling to it.
/// </summary>
public sealed class UserId : ValueObject
{
    public Guid Value { get; }

    private UserId(Guid value)
    {
        Value = value;
    }

    public static UserId Create(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty.", nameof(value));

        return new UserId(value);
    }

    public static UserId CreateNew() => new(Guid.NewGuid());

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(UserId userId) => userId.Value;
}

