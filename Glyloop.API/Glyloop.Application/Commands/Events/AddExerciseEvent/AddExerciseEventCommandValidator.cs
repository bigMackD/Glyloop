using FluentValidation;

namespace Glyloop.Application.Commands.Events.AddExerciseEvent;

/// <summary>
/// Validator for AddExerciseEventCommand.
/// Validates exercise duration, event time, and optional fields.
/// </summary>
public class AddExerciseEventCommandValidator : AbstractValidator<AddExerciseEventCommand>
{
    public AddExerciseEventCommandValidator()
    {
        RuleFor(x => x.ExerciseTypeId)
            .GreaterThan(0)
            .WithMessage("Exercise type ID must be a positive number.");

        RuleFor(x => x.DurationMinutes)
            .InclusiveBetween(1, 300)
            .WithMessage("Exercise duration must be between 1 and 300 minutes.");

        RuleFor(x => x.EventTime)
            .LessThanOrEqualTo(DateTimeOffset.UtcNow)
            .WithMessage("Event time cannot be in the future.");

        RuleFor(x => x.Intensity)
            .IsInEnum()
            .WithMessage("Intensity must be a valid value (Light, Moderate, Vigorous).")
            .When(x => x.Intensity.HasValue);

        RuleFor(x => x.Note)
            .MaximumLength(500)
            .WithMessage("Note must not exceed 500 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Note));
    }
}

