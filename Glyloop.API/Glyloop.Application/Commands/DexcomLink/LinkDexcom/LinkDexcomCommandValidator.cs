using FluentValidation;

namespace Glyloop.Application.Commands.DexcomLink.LinkDexcom;

/// <summary>
/// Validator for LinkDexcomCommand.
/// Validates authorization code format and presence.
/// </summary>
public class LinkDexcomCommandValidator : AbstractValidator<LinkDexcomCommand>
{
    public LinkDexcomCommandValidator()
    {
        RuleFor(x => x.AuthorizationCode)
            .NotEmpty()
            .WithMessage("Authorization code is required.")
            .MaximumLength(500)
            .WithMessage("Authorization code must not exceed 500 characters.");
    }
}

