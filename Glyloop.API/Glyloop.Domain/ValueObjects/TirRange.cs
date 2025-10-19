using Glyloop.Domain.Common;
using Glyloop.Domain.Errors;

namespace Glyloop.Domain.ValueObjects;

/// <summary>
/// Represents a Time in Range (TIR) configuration with lower and upper glucose bounds (mg/dL).
/// Invariant: Lower must be less than Upper, and both must be between 0 and 1000.
/// Reference: DDD Plan Section 3 - Value Objects
/// </summary>
public sealed class TirRange : ValueObject
{
    public int Lower { get; }
    public int Upper { get; }

    private TirRange(int lower, int upper)
    {
        Lower = lower;
        Upper = upper;
    }

    /// <summary>
    /// Creates a TIR range with validation.
    /// </summary>
    /// <param name="lower">Lower bound in mg/dL (0-1000)</param>
    /// <param name="upper">Upper bound in mg/dL (0-1000)</param>
    /// <returns>Result containing TirRange or validation error</returns>
    public static Result<TirRange> Create(int lower, int upper)
    {
        if (lower < 0 || lower > 1000 || upper < 0 || upper > 1000)
            return Result.Failure<TirRange>(DomainErrors.User.InvalidTirRange);

        if (lower >= upper)
            return Result.Failure<TirRange>(DomainErrors.User.InvalidTirRange);

        return Result.Success(new TirRange(lower, upper));
    }

    /// <summary>
    /// Creates a standard TIR range (70-180 mg/dL) as recommended by clinical guidelines.
    /// </summary>
    public static TirRange Standard() => new(70, 180);

    /// <summary>
    /// Checks if a glucose value falls within this TIR range.
    /// </summary>
    public bool IsInRange(int glucoseValue) => glucoseValue >= Lower && glucoseValue <= Upper;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Lower;
        yield return Upper;
    }

    public override string ToString() => $"{Lower}-{Upper} mg/dL";
}

