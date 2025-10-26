using Glyloop.Application.Common.Interfaces;
using Glyloop.Application.DTOs.Events;
using Glyloop.Domain.Aggregates.Event;
using Glyloop.Domain.Common;
using Glyloop.Domain.Enums;
using Glyloop.Domain.Repositories;
using Glyloop.Domain.ValueObjects;
using MediatR;

namespace Glyloop.Application.Commands.Events.AddNoteEvent;

/// <summary>
/// Handler for AddNoteEventCommand.
/// Creates a note event aggregate and persists it to the repository.
/// </summary>
public class AddNoteEventCommandHandler : IRequestHandler<AddNoteEventCommand, Result<NoteEventDto>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITimeProvider _timeProvider;

    public AddNoteEventCommandHandler(
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

    public async Task<Result<NoteEventDto>> Handle(
        AddNoteEventCommand request,
        CancellationToken cancellationToken)
    {
        var userId = UserId.Create(_currentUserService.UserId);

        var textResult = NoteText.Create(request.Text);
        if (textResult.IsFailure)
        {
            return Result.Failure<NoteEventDto>(textResult.Error);
        }

        var correlationId = Guid.NewGuid();
        var causationId = Guid.NewGuid();

        var noteEventResult = NoteEvent.Create(
            userId,
            request.EventTime,
            textResult.Value,
            SourceType.Manual,
            _timeProvider,
            correlationId,
            causationId);

        if (noteEventResult.IsFailure)
        {
            return Result.Failure<NoteEventDto>(noteEventResult.Error);
        }

        var noteEvent = noteEventResult.Value;

        _eventRepository.Add(noteEvent);

        await _unitOfWork.SaveEntitiesAsync(cancellationToken);

        var dto = new NoteEventDto(
            noteEvent.Id,
            userId.Value,
            noteEvent.EventType,
            noteEvent.EventTime,
            noteEvent.CreatedAt,
            null, // Note field is null for NoteEvent (Text is the primary content)
            noteEvent.Text.Text);

        return Result.Success(dto);
    }
}

