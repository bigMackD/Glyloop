using FluentValidation;

namespace Glyloop.Application.Commands.Events.AddNoteEvent;

/// <summary>
/// Validator for AddNoteEventCommand.
/// Validates note text length and event time.
/// </summary>
public class AddNoteEventCommandValidator : AbstractValidator<AddNoteEventCommand>
{
    public AddNoteEventCommandValidator()
    {
        RuleFor(x => x.Text)
            .NotEmpty()
            .WithMessage("Note text is required.")
            .Length(1, 500)
            .WithMessage("Note text must be between 1 and 500 characters.");

        RuleFor(x => x.EventTime)
            .LessThanOrEqualTo(DateTimeOffset.UtcNow)
            .WithMessage("Event time cannot be in the future.");
    }
}

