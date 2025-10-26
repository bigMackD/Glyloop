using Glyloop.Application.Common.Interfaces;
using Glyloop.Application.DTOs.DexcomLink;
using Glyloop.Domain.Common;
using Glyloop.Domain.Repositories;
using Glyloop.Domain.ValueObjects;
using MediatR;

namespace Glyloop.Application.Queries.DexcomLink.GetDexcomLinks;

/// <summary>
/// Handler for GetDexcomLinksQuery.
/// Retrieves all Dexcom links (active and historical) for the current user.
/// </summary>
public class GetDexcomLinksQueryHandler : IRequestHandler<GetDexcomLinksQuery, Result<List<DexcomLinkDto>>>
{
    private readonly IDexcomLinkRepository _dexcomLinkRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetDexcomLinksQueryHandler(
        IDexcomLinkRepository dexcomLinkRepository,
        ICurrentUserService currentUserService)
    {
        _dexcomLinkRepository = dexcomLinkRepository;
        _currentUserService = currentUserService;
    }

    public async Task<Result<List<DexcomLinkDto>>> Handle(
        GetDexcomLinksQuery request,
        CancellationToken cancellationToken)
    {
        // Get current user ID (guaranteed by API layer authentication)
        var userId = UserId.Create(_currentUserService.UserId);

        // Get all links for user
        var links = await _dexcomLinkRepository.GetByUserIdAsync(userId, cancellationToken);

        // Map to DTOs
        var dtos = links.Select(link => new DexcomLinkDto(
            link.Id,
            userId.Value,
            link.TokenExpiresAt,
            link.LastRefreshedAt,
            link.IsActive,
            link.ShouldRefresh))
            .ToList();

        return Result.Success(dtos);
    }
}

