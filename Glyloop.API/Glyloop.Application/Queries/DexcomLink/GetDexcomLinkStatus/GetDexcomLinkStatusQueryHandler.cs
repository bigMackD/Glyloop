using Glyloop.Application.Common.Interfaces;
using Glyloop.Application.DTOs.DexcomLink;
using Glyloop.Domain.Common;
using Glyloop.Domain.Repositories;
using Glyloop.Domain.ValueObjects;
using MediatR;

namespace Glyloop.Application.Queries.DexcomLink.GetDexcomLinkStatus;

/// <summary>
/// Handler for GetDexcomLinkStatusQuery.
/// Retrieves the active Dexcom link status for the current user.
/// </summary>
public class GetDexcomLinkStatusQueryHandler : IRequestHandler<GetDexcomLinkStatusQuery, Result<DexcomLinkStatusDto>>
{
    private readonly IDexcomLinkRepository _dexcomLinkRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetDexcomLinkStatusQueryHandler(
        IDexcomLinkRepository dexcomLinkRepository,
        ICurrentUserService currentUserService)
    {
        _dexcomLinkRepository = dexcomLinkRepository;
        _currentUserService = currentUserService;
    }

    public async Task<Result<DexcomLinkStatusDto>> Handle(
        GetDexcomLinkStatusQuery request,
        CancellationToken cancellationToken)
    {
        // Get current user ID (guaranteed by API layer authentication)
        var userId = UserId.Create(_currentUserService.UserId);

        // Get active link for user
        var link = await _dexcomLinkRepository.GetActiveByUserIdAsync(userId, cancellationToken);

        // Map to DTO
        DexcomLinkStatusDto dto;
        if (link == null)
        {
            // No active link
            dto = new DexcomLinkStatusDto(
                IsLinked: false,
                LinkId: null,
                TokenExpiresAt: null,
                LastRefreshedAt: null,
                ShouldRefresh: null);
        }
        else
        {
            // Active link exists
            dto = new DexcomLinkStatusDto(
                IsLinked: true,
                LinkId: link.Id,
                TokenExpiresAt: link.TokenExpiresAt,
                LastRefreshedAt: link.LastRefreshedAt,
                ShouldRefresh: link.ShouldRefresh);
        }

        return Result.Success(dto);
    }
}

