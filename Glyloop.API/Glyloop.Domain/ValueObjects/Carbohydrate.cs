using Glyloop.Domain.Common;
using Glyloop.Domain.Errors;

namespace Glyloop.Domain.ValueObjects;

/// <summary>
/// Represents a carbohydrate amount in grams for food events.
/// Invariant: Must be between 0 and 300 grams.
/// Reference: DDD Plan Section 3 - Value Objects
/// </summary>
public sealed class Carbohydrate : ValueObject
{
    public int Grams { get; }

    private Carbohydrate(int grams)
    {
        Grams = grams;
    }

    /// <summary>
    /// Creates a carbohydrate value object with validation.
    /// </summary>
    /// <param name="grams">Amount in grams (0-300)</param>
    public static Result<Carbohydrate> Create(int grams)
    {
        if (grams < 0 || grams > 300)
            return Result.Failure<Carbohydrate>(DomainErrors.Event.InvalidCarbohydrates);

        return Result.Success(new Carbohydrate(grams));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Grams;
    }

    public override string ToString() => $"{Grams}g";

    public static implicit operator int(Carbohydrate carb) => carb.Grams;
}

