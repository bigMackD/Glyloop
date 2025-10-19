using Glyloop.Application.Common.Interfaces;
using Glyloop.Application.DTOs.Events;
using Glyloop.Domain.Aggregates.Event;
using Glyloop.Domain.Common;
using Glyloop.Domain.Enums;
using Glyloop.Domain.Repositories;
using Glyloop.Domain.ValueObjects;
using MediatR;

namespace Glyloop.Application.Commands.Events.AddExerciseEvent;

/// <summary>
/// Handler for AddExerciseEventCommand.
/// Creates an exercise event aggregate and persists it to the repository.
/// </summary>
public class AddExerciseEventCommandHandler : IRequestHandler<AddExerciseEventCommand, Result<ExerciseEventDto>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITimeProvider _timeProvider;

    public AddExerciseEventCommandHandler(
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

    public async Task<Result<ExerciseEventDto>> Handle(
        AddExerciseEventCommand request,
        CancellationToken cancellationToken)
    {
        var userId = UserId.Create(_currentUserService.UserId);

        var durationResult = ExerciseDuration.Create(request.DurationMinutes);
        if (durationResult.IsFailure)
        {
            return Result.Failure<ExerciseEventDto>(durationResult.Error);
        }

        var exerciseTypeId = ExerciseTypeId.Create(request.ExerciseTypeId);

        var intensity = request.Intensity ?? IntensityType.Moderate;

        var note = NoteText.CreateOptional(request.Note);

        var correlationId = Guid.NewGuid();
        var causationId = Guid.NewGuid();

        var exerciseEventResult = ExerciseEvent.Create(
            userId,
            request.EventTime,
            exerciseTypeId,
            durationResult.Value,
            intensity,
            note,
            SourceType.Manual,
            _timeProvider,
            correlationId,
            causationId);

        if (exerciseEventResult.IsFailure)
        {
            return Result.Failure<ExerciseEventDto>(exerciseEventResult.Error);
        }

        var exerciseEvent = exerciseEventResult.Value;

        _eventRepository.Add(exerciseEvent);

        await _unitOfWork.SaveEntitiesAsync(cancellationToken);

        var dto = new ExerciseEventDto(
            exerciseEvent.Id,
            userId.Value,
            exerciseEvent.EventType,
            exerciseEvent.EventTime,
            exerciseEvent.CreatedAt,
            exerciseEvent.Note?.Text,
            exerciseEvent.ExerciseType.Value,
            exerciseEvent.Duration.Minutes,
            exerciseEvent.Intensity);

        return Result.Success(dto);
    }
}

