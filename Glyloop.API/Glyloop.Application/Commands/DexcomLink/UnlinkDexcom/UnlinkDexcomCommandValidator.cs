using FluentValidation;

namespace Glyloop.Application.Commands.DexcomLink.UnlinkDexcom;

/// <summary>
/// Validator for UnlinkDexcomCommand.
/// Validates link ID is provided.
/// </summary>
public class UnlinkDexcomCommandValidator : AbstractValidator<UnlinkDexcomCommand>
{
    public UnlinkDexcomCommandValidator()
    {
        RuleFor(x => x.LinkId)
            .NotEmpty()
            .WithMessage("Link ID is required.");
    }
}

