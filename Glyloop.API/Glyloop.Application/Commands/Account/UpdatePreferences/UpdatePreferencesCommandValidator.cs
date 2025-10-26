using FluentValidation;

namespace Glyloop.Application.Commands.Account.UpdatePreferences;

/// <summary>
/// Validator for UpdatePreferencesCommand.
/// Validates TIR range bounds and cross-field validation.
/// </summary>
public class UpdatePreferencesCommandValidator : AbstractValidator<UpdatePreferencesCommand>
{
    public UpdatePreferencesCommandValidator()
    {
        RuleFor(x => x.LowerBound)
            .InclusiveBetween(0, 1000)
            .WithMessage("Lower bound must be between 0 and 1000 mg/dL.");

        RuleFor(x => x.UpperBound)
            .InclusiveBetween(0, 1000)
            .WithMessage("Upper bound must be between 0 and 1000 mg/dL.");

        RuleFor(x => x)
            .Must(x => x.LowerBound < x.UpperBound)
            .WithMessage("Lower bound must be less than upper bound.")
            .When(x => x.LowerBound >= 0 && x.UpperBound <= 1000);
    }
}

