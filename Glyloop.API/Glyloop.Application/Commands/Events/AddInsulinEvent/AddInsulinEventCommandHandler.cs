using Glyloop.Application.Common.Interfaces;
using Glyloop.Application.DTOs.Events;
using Glyloop.Domain.Aggregates.Event;
using Glyloop.Domain.Common;
using Glyloop.Domain.Enums;
using Glyloop.Domain.Repositories;
using Glyloop.Domain.ValueObjects;
using MediatR;

namespace Glyloop.Application.Commands.Events.AddInsulinEvent;

/// <summary>
/// Handler for AddInsulinEventCommand.
/// Creates an insulin event aggregate and persists it to the repository.
/// </summary>
public class AddInsulinEventCommandHandler : IRequestHandler<AddInsulinEventCommand, Result<InsulinEventDto>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITimeProvider _timeProvider;

    public AddInsulinEventCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ITimeProvider timeProvider)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _timeProvider = timeProvider;
    }

    public async Task<Result<InsulinEventDto>> Handle(
        AddInsulinEventCommand request,
        CancellationToken cancellationToken)
    {
        var userId = UserId.Create(_currentUserService.UserId);

        var doseResult = InsulinDose.Create(request.Units);
        if (doseResult.IsFailure)
        {
            return Result.Failure<InsulinEventDto>(doseResult.Error);
        }

        var note = NoteText.CreateOptional(request.Note);

        var correlationId = Guid.NewGuid();
        var causationId = Guid.NewGuid();

        var insulinEventResult = InsulinEvent.Create(
            userId,
            request.EventTime,
            request.InsulinType,
            doseResult.Value,
            request.Preparation,
            request.Delivery,
            request.Timing,
            note,
            SourceType.Manual,
            _timeProvider,
            correlationId,
            causationId);

        if (insulinEventResult.IsFailure)
        {
            return Result.Failure<InsulinEventDto>(insulinEventResult.Error);
        }

        var insulinEvent = insulinEventResult.Value;

        _eventRepository.Add(insulinEvent);

        await _unitOfWork.SaveEntitiesAsync(cancellationToken);

        var dto = new InsulinEventDto(
            insulinEvent.Id,
            userId.Value,
            insulinEvent.EventType,
            insulinEvent.EventTime,
            insulinEvent.CreatedAt,
            insulinEvent.Note?.Text,
            insulinEvent.InsulinType,
            insulinEvent.Dose.Units,
            insulinEvent.Preparation,
            insulinEvent.Delivery,
            insulinEvent.Timing);

        return Result.Success(dto);
    }
}

