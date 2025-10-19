using FluentValidation;

namespace Glyloop.Application.Commands.Events.AddFoodEvent;

/// <summary>
/// Validator for AddFoodEventCommand.
/// Validates carbohydrate range, event time, and optional fields.
/// </summary>
public class AddFoodEventCommandValidator : AbstractValidator<AddFoodEventCommand>
{
    public AddFoodEventCommandValidator()
    {
        RuleFor(x => x.CarbohydratesGrams)
            .InclusiveBetween(0, 300)
            .WithMessage("Carbohydrates must be between 0 and 300 grams.");

        RuleFor(x => x.EventTime)
            .LessThanOrEqualTo(DateTimeOffset.UtcNow)
            .WithMessage("Event time cannot be in the future.");

        RuleFor(x => x.MealTagId)
            .GreaterThan(0)
            .WithMessage("Meal tag ID must be a positive number.")
            .When(x => x.MealTagId.HasValue);

        RuleFor(x => x.AbsorptionHint)
            .IsInEnum()
            .WithMessage("Absorption hint must be a valid value (Rapid, Normal, Slow, Other).")
            .When(x => x.AbsorptionHint.HasValue);

        RuleFor(x => x.Note)
            .MaximumLength(500)
            .WithMessage("Note must not exceed 500 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Note));
    }
}

