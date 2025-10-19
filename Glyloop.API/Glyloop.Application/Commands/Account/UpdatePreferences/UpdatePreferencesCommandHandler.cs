using Glyloop.Application.Common.Interfaces;
using Glyloop.Application.DTOs.Account;
using Glyloop.Domain.Common;
using Glyloop.Domain.ValueObjects;
using MediatR;

namespace Glyloop.Application.Commands.Account.UpdatePreferences;

/// <summary>
/// Handler for UpdatePreferencesCommand.
/// Updates the user's TIR range preferences.
/// </summary>
public class UpdatePreferencesCommandHandler : IRequestHandler<UpdatePreferencesCommand, Result<UserPreferencesDto>>
{
    private readonly IIdentityService _identityService;
    private readonly ICurrentUserService _currentUserService;

    public UpdatePreferencesCommandHandler(
        IIdentityService identityService,
        ICurrentUserService currentUserService)
    {
        _identityService = identityService;
        _currentUserService = currentUserService;
    }

    public async Task<Result<UserPreferencesDto>> Handle(
        UpdatePreferencesCommand request,
        CancellationToken cancellationToken)
    {
        var userId = UserId.Create(_currentUserService.UserId);

        var tirRangeResult = TirRange.Create(request.LowerBound, request.UpperBound);
        if (tirRangeResult.IsFailure)
        {
            return Result.Failure<UserPreferencesDto>(tirRangeResult.Error);
        }

        var updateResult = await _identityService.UpdateUserPreferencesAsync(
            userId,
            tirRangeResult.Value,
            cancellationToken);

        if (updateResult.IsFailure)
        {
            return Result.Failure<UserPreferencesDto>(updateResult.Error);
        }

        var dto = new UserPreferencesDto(
            userId.Value,
            tirRangeResult.Value.Lower,
            tirRangeResult.Value.Upper);

        return Result.Success(dto);
    }
}

