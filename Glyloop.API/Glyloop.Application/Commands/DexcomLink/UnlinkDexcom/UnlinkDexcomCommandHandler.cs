using Glyloop.Application.Common.Interfaces;
using Glyloop.Domain.Common;
using Glyloop.Domain.Errors;
using Glyloop.Domain.Repositories;
using Glyloop.Domain.ValueObjects;
using MediatR;

namespace Glyloop.Application.Commands.DexcomLink.UnlinkDexcom;

/// <summary>
/// Handler for UnlinkDexcomCommand.
/// Removes the Dexcom link and optionally purges associated CGM data.
/// </summary>
public class UnlinkDexcomCommandHandler : IRequestHandler<UnlinkDexcomCommand, Result>
{
    private readonly IDexcomLinkRepository _dexcomLinkRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public UnlinkDexcomCommandHandler(
        IDexcomLinkRepository dexcomLinkRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _dexcomLinkRepository = dexcomLinkRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<Result> Handle(
        UnlinkDexcomCommand request,
        CancellationToken cancellationToken)
    {
        var userId = UserId.Create(_currentUserService.UserId);

        var link = await _dexcomLinkRepository.GetByIdAsync(request.LinkId, cancellationToken);
        if (link == null)
        {
            return Result.Failure(DomainErrors.DexcomLink.LinkNotFound);
        }

        if (link.UserId.Value != userId.Value)
        {
            return Result.Failure(
                Error.Create("Authorization.Forbidden", "User does not own this Dexcom link."));
        }

        var correlationId = Guid.NewGuid();
        var causationId = Guid.NewGuid();
        link.Unlink(request.PurgeData, correlationId, causationId);

        _dexcomLinkRepository.Remove(link);

        await _unitOfWork.SaveEntitiesAsync(cancellationToken);

        return Result.Success();
    }
}

