using FluentValidation;

namespace Glyloop.Application.Queries.Chart.GetTimeInRange;

/// <summary>
/// Validator for GetTimeInRangeQuery.
/// Validates time range parameters.
/// </summary>
public class GetTimeInRangeQueryValidator : AbstractValidator<GetTimeInRangeQuery>
{
    public GetTimeInRangeQueryValidator()
    {
        RuleFor(x => x.FromTime)
            .NotEmpty()
            .WithMessage("From time is required.");

        RuleFor(x => x.ToTime)
            .NotEmpty()
            .WithMessage("To time is required.");

        RuleFor(x => x)
            .Must(x => x.FromTime < x.ToTime)
            .WithMessage("From time must be before To time.");
    }
}

