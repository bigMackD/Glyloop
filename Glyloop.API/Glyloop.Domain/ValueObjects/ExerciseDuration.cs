using Glyloop.Domain.Common;
using Glyloop.Domain.Errors;

namespace Glyloop.Domain.ValueObjects;

/// <summary>
/// Represents the duration of an exercise session in minutes.
/// Invariant: Must be between 1 and 300 minutes.
/// Reference: DDD Plan Section 3 - Value Objects
/// </summary>
public sealed class ExerciseDuration : ValueObject
{
    public int Minutes { get; }

    private ExerciseDuration(int minutes)
    {
        Minutes = minutes;
    }

    /// <summary>
    /// Creates an exercise duration value object with validation.
    /// </summary>
    /// <param name="minutes">Duration in minutes (1-300)</param>
    public static Result<ExerciseDuration> Create(int minutes)
    {
        if (minutes < 1 || minutes > 300)
            return Result.Failure<ExerciseDuration>(DomainErrors.Event.InvalidExerciseDuration);

        return Result.Success(new ExerciseDuration(minutes));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Minutes;
    }

    public override string ToString() => $"{Minutes} min";

    public static implicit operator int(ExerciseDuration duration) => duration.Minutes;
}

