using Glyloop.Domain.Common;
using Glyloop.Domain.Errors;

namespace Glyloop.Domain.ValueObjects;

/// <summary>
/// Represents an insulin dose in units.
/// Invariant: Must be between 0 and 100 units in 0.5 unit increments.
/// Reference: DDD Plan Section 3 - Value Objects
/// </summary>
public sealed class InsulinDose : ValueObject
{
    public decimal Units { get; }

    private InsulinDose(decimal units)
    {
        Units = units;
    }

    /// <summary>
    /// Creates an insulin dose value object with validation.
    /// </summary>
    /// <param name="units">Dose in units (0-100, 0.5 increments)</param>
    public static Result<InsulinDose> Create(decimal units)
    {
        if (units < 0 || units > 100)
            return Result.Failure<InsulinDose>(DomainErrors.Event.InvalidInsulinDose);

        // Check for 0.5 unit increments
        if ((units * 2) % 1 != 0)
            return Result.Failure<InsulinDose>(DomainErrors.Event.InvalidInsulinDose);

        return Result.Success(new InsulinDose(units));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Units;
    }

    public override string ToString() => $"{Units}U";

    public static implicit operator decimal(InsulinDose dose) => dose.Units;
}

