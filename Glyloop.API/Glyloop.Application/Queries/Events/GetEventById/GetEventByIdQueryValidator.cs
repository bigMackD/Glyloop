using FluentValidation;

namespace Glyloop.Application.Queries.Events.GetEventById;

/// <summary>
/// Validator for GetEventByIdQuery.
/// Validates that EventId is provided.
/// </summary>
public class GetEventByIdQueryValidator : AbstractValidator<GetEventByIdQuery>
{
    public GetEventByIdQueryValidator()
    {
        RuleFor(x => x.EventId)
            .NotEmpty()
            .WithMessage("Event ID is required.");
    }
}

