using FluentValidation;

namespace Glyloop.Application.Queries.Events.GetEventOutcome;

/// <summary>
/// Validator for GetEventOutcomeQuery.
/// Validates that EventId is provided.
/// </summary>
public class GetEventOutcomeQueryValidator : AbstractValidator<GetEventOutcomeQuery>
{
    public GetEventOutcomeQueryValidator()
    {
        RuleFor(x => x.EventId)
            .NotEmpty()
            .WithMessage("Event ID is required.");
    }
}

