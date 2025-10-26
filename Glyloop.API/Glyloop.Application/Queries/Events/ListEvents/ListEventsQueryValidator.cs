using FluentValidation;

namespace Glyloop.Application.Queries.Events.ListEvents;

/// <summary>
/// Validator for ListEventsQuery.
/// Validates pagination parameters and date range.
/// </summary>
public class ListEventsQueryValidator : AbstractValidator<ListEventsQuery>
{
    public ListEventsQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Page must be at least 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("Page size must be between 1 and 100.");

        RuleFor(x => x.EventType)
            .IsInEnum()
            .WithMessage("Event type must be a valid value (Food, Insulin, Exercise, Note).")
            .When(x => x.EventType.HasValue);

        RuleFor(x => x)
            .Must(x => !x.FromDate.HasValue || !x.ToDate.HasValue || x.FromDate.Value <= x.ToDate.Value)
            .WithMessage("From date must be before or equal to To date.")
            .When(x => x.FromDate.HasValue && x.ToDate.HasValue);
    }
}

