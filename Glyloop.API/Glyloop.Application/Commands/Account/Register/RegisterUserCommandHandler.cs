using Glyloop.Application.Common.Interfaces;
using Glyloop.Application.DTOs.Account;
using Glyloop.Domain.Common;
using MediatR;

namespace Glyloop.Application.Commands.Account.Register;

/// <summary>
/// Handler for RegisterUserCommand.
/// Creates a new user account using ASP.NET Core Identity.
/// </summary>
public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Result<UserRegisteredDto>>
{
    private readonly IIdentityService _identityService;

    public RegisterUserCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<Result<UserRegisteredDto>> Handle(
        RegisterUserCommand request,
        CancellationToken cancellationToken)
    {
        var createResult = await _identityService.CreateUserAsync(
            request.Email,
            request.Password,
            cancellationToken);

        if (createResult.IsFailure)
        {
            return Result.Failure<UserRegisteredDto>(createResult.Error);
        }

        var dto = new UserRegisteredDto(
            createResult.Value,
            request.Email,
            DateTimeOffset.UtcNow);

        return Result.Success(dto);
    }
}

