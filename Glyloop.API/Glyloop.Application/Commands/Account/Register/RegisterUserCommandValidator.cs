using FluentValidation;
using Glyloop.Application.Common.Interfaces;

namespace Glyloop.Application.Commands.Account.Register;

/// <summary>
/// Validator for RegisterUserCommand.
/// Validates email format, password strength, and email uniqueness.
/// </summary>
public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    private readonly IIdentityService _identityService;

    public RegisterUserCommandValidator(IIdentityService identityService)
    {
        _identityService = identityService;

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required.")
            .EmailAddress()
            .WithMessage("Email must be a valid email address.")
            .MaximumLength(255)
            .WithMessage("Email must not exceed 255 characters.");

        RuleFor(x => x.Email)
            .MustAsync(BeUniqueEmail)
            .WithMessage("Email is already registered.");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password is required.")
            .MinimumLength(12)
            .WithMessage("Password must be at least 12 characters long.")
            .MaximumLength(128)
            .WithMessage("Password must not exceed 128 characters.");
    }

    private async Task<bool> BeUniqueEmail(string email, CancellationToken cancellationToken)
    {
        var exists = await _identityService.EmailExistsAsync(email, cancellationToken);
        return !exists;
    }
}

