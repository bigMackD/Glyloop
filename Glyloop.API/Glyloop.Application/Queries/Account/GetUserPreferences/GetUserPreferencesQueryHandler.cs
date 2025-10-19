using Glyloop.Application.Common.Interfaces;
using Glyloop.Application.DTOs.Account;
using Glyloop.Domain.Common;
using Glyloop.Domain.ValueObjects;
using MediatR;

namespace Glyloop.Application.Queries.Account.GetUserPreferences;

/// <summary>
/// Handler for GetUserPreferencesQuery.
/// Retrieves the current user's TIR range preferences.
/// </summary>
public class GetUserPreferencesQueryHandler : IRequestHandler<GetUserPreferencesQuery, Result<UserPreferencesDto>>
{
    private readonly IIdentityService _identityService;
    private readonly ICurrentUserService _currentUserService;

    public GetUserPreferencesQueryHandler(
        IIdentityService identityService,
        ICurrentUserService currentUserService)
    {
        _identityService = identityService;
        _currentUserService = currentUserService;
    }

    public async Task<Result<UserPreferencesDto>> Handle(
        GetUserPreferencesQuery request,
        CancellationToken cancellationToken)
    {
        // Get current user ID (guaranteed by API layer authentication)
        var userId = UserId.Create(_currentUserService.UserId);

        // Get preferences via identity service
        var preferencesResult = await _identityService.GetUserPreferencesAsync(userId, cancellationToken);

        if (preferencesResult.IsFailure)
        {
            return Result.Failure<UserPreferencesDto>(preferencesResult.Error);
        }

        // Map to DTO
        var dto = new UserPreferencesDto(
            userId.Value,
            preferencesResult.Value.Lower,
            preferencesResult.Value.Upper);

        return Result.Success(dto);
    }
}

