using Glyloop.Domain.Common;

namespace Glyloop.Domain.ValueObjects;

/// <summary>
/// Represents a reference to a meal tag/category (e.g., Breakfast, Lunch, Dinner, Snack).
/// The actual meal tag definitions are managed in a separate bounded context or reference data.
/// </summary>
public sealed class MealTagId : ValueObject
{
    public int Value { get; }

    private MealTagId(int value)
    {
        Value = value;
    }

    public static MealTagId Create(int value)
    {
        if (value <= 0)
            throw new ArgumentException("Meal tag ID must be positive.", nameof(value));

        return new MealTagId(value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();

    public static implicit operator int(MealTagId mealTagId) => mealTagId.Value;
}

