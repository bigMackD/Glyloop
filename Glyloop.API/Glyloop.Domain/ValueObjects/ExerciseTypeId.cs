using Glyloop.Domain.Common;

namespace Glyloop.Domain.ValueObjects;

/// <summary>
/// Represents a reference to an exercise type (e.g., Walking, Running, Cycling, Swimming).
/// The actual exercise type definitions are managed in a separate bounded context or reference data.
/// </summary>
public sealed class ExerciseTypeId : ValueObject
{
    public int Value { get; }

    private ExerciseTypeId(int value)
    {
        Value = value;
    }

    public static ExerciseTypeId Create(int value)
    {
        if (value <= 0)
            throw new ArgumentException("Exercise type ID must be positive.", nameof(value));

        return new ExerciseTypeId(value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();

    public static implicit operator int(ExerciseTypeId exerciseTypeId) => exerciseTypeId.Value;
}

