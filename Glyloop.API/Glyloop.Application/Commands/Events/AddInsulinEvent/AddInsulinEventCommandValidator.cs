using FluentValidation;

namespace Glyloop.Application.Commands.Events.AddInsulinEvent;

/// <summary>
/// Validator for AddInsulinEventCommand.
/// Validates insulin dose range, event time, and optional fields.
/// </summary>
public class AddInsulinEventCommandValidator : AbstractValidator<AddInsulinEventCommand>
{
    public AddInsulinEventCommandValidator()
    {
        RuleFor(x => x.InsulinType)
            .IsInEnum()
            .WithMessage("Insulin type must be either Fast or Long.");

        RuleFor(x => x.Units)
            .InclusiveBetween(0m, 100m)
            .WithMessage("Insulin dose must be between 0 and 100 units.");

        RuleFor(x => x.Units)
            .Must(BeInHalfUnitIncrements)
            .WithMessage("Insulin dose must be in 0.5 unit increments.");

        RuleFor(x => x.EventTime)
            .LessThanOrEqualTo(DateTimeOffset.UtcNow)
            .WithMessage("Event time cannot be in the future.");

        RuleFor(x => x.Preparation)
            .MaximumLength(100)
            .WithMessage("Preparation details must not exceed 100 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Preparation));

        RuleFor(x => x.Delivery)
            .MaximumLength(100)
            .WithMessage("Delivery details must not exceed 100 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Delivery));

        RuleFor(x => x.Timing)
            .MaximumLength(50)
            .WithMessage("Timing details must not exceed 50 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Timing));

        RuleFor(x => x.Note)
            .MaximumLength(500)
            .WithMessage("Note must not exceed 500 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Note));
    }

    private bool BeInHalfUnitIncrements(decimal units)
    {
        return (units * 2) % 1 == 0;
    }
}

