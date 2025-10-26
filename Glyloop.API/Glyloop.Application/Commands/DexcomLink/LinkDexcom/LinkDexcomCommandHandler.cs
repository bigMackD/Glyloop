using Glyloop.Application.Common.Interfaces;
using Glyloop.Application.DTOs.DexcomLink;
using Glyloop.Domain.Common;
using Glyloop.Domain.Repositories;
using Glyloop.Domain.ValueObjects;
using MediatR;

namespace Glyloop.Application.Commands.DexcomLink.LinkDexcom;

/// <summary>
/// Handler for LinkDexcomCommand.
/// Orchestrates OAuth token exchange, token encryption, and DexcomLink aggregate creation.
/// </summary>
public class LinkDexcomCommandHandler : IRequestHandler<LinkDexcomCommand, Result<DexcomLinkCreatedDto>>
{
    private readonly IDexcomService _dexcomService;
    private readonly ITokenEncryptionService _tokenEncryptionService;
    private readonly IDexcomLinkRepository _dexcomLinkRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITimeProvider _timeProvider;

    public LinkDexcomCommandHandler(
        IDexcomService dexcomService,
        ITokenEncryptionService tokenEncryptionService,
        IDexcomLinkRepository dexcomLinkRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ITimeProvider timeProvider)
    {
        _dexcomService = dexcomService;
        _tokenEncryptionService = tokenEncryptionService;
        _dexcomLinkRepository = dexcomLinkRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _timeProvider = timeProvider;
    }

    public async Task<Result<DexcomLinkCreatedDto>> Handle(
        LinkDexcomCommand request,
        CancellationToken cancellationToken)
    {
        var userId = UserId.Create(_currentUserService.UserId);

        var existingLink = await _dexcomLinkRepository.GetActiveByUserIdAsync(userId, cancellationToken);
        if (existingLink != null)
        {
            return Result.Failure<DexcomLinkCreatedDto>(
                Error.Create("DexcomLink.AlreadyLinked", "User already has an active Dexcom link."));
        }

        var tokensResult = await _dexcomService.ExchangeCodeForTokensAsync(
            request.AuthorizationCode,
            cancellationToken);

        if (tokensResult.IsFailure)
        {
            return Result.Failure<DexcomLinkCreatedDto>(tokensResult.Error);
        }

        var tokens = tokensResult.Value;

        var encryptedAccessToken = _tokenEncryptionService.Encrypt(tokens.AccessToken);
        var encryptedRefreshToken = _tokenEncryptionService.Encrypt(tokens.RefreshToken);

        var tokenExpiresAt = _timeProvider.UtcNow.AddSeconds(tokens.ExpiresInSeconds);

        var correlationId = Guid.NewGuid();
        var causationId = Guid.NewGuid();

        var linkResult = Domain.Aggregates.DexcomLink.DexcomLink.Create(
            userId,
            encryptedAccessToken,
            encryptedRefreshToken,
            tokenExpiresAt,
            _timeProvider,
            correlationId,
            causationId);

        if (linkResult.IsFailure)
        {
            return Result.Failure<DexcomLinkCreatedDto>(linkResult.Error);
        }

        var link = linkResult.Value;

        _dexcomLinkRepository.Add(link);

        await _unitOfWork.SaveEntitiesAsync(cancellationToken);

        var dto = new DexcomLinkCreatedDto(
            link.Id,
            userId.Value,
            link.TokenExpiresAt,
            _timeProvider.UtcNow);

        return Result.Success(dto);
    }
}

