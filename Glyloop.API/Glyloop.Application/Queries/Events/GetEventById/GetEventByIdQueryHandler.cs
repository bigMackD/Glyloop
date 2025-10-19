using Glyloop.Application.Common.Interfaces;
using Glyloop.Application.DTOs.Events;
using Glyloop.Domain.Aggregates.Event;
using Glyloop.Domain.Common;
using Glyloop.Domain.Repositories;
using Glyloop.Domain.ValueObjects;
using MediatR;

namespace Glyloop.Application.Queries.Events.GetEventById;

/// <summary>
/// Handler for GetEventByIdQuery.
/// Retrieves event details and verifies user ownership.
/// </summary>
public class GetEventByIdQueryHandler : IRequestHandler<GetEventByIdQuery, Result<EventDto>>
{
    private readonly IEventRepository _eventRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetEventByIdQueryHandler(
        IEventRepository eventRepository,
        ICurrentUserService currentUserService)
    {
        _eventRepository = eventRepository;
        _currentUserService = currentUserService;
    }

    public async Task<Result<EventDto>> Handle(
        GetEventByIdQuery request,
        CancellationToken cancellationToken)
    {
        var userId = UserId.Create(_currentUserService.UserId);

        var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        
        if (@event == null)
        {
            return Result.Failure<EventDto>(
                Error.Create("Event.NotFound", "Event not found."));
        }

        if (@event.UserId.Value != userId.Value)
        {
            return Result.Failure<EventDto>(
                Error.Create("Authorization.Forbidden", "User does not own this event."));
        }

        EventDto dto = @event switch
        {
            FoodEvent food => new FoodEventDto(
                food.Id,
                userId.Value,
                food.EventType,
                food.EventTime,
                food.CreatedAt,
                food.Note?.Text,
                food.Carbohydrates.Grams,
                food.MealTag.Value,
                food.AbsorptionHint),

            InsulinEvent insulin => new InsulinEventDto(
                insulin.Id,
                userId.Value,
                insulin.EventType,
                insulin.EventTime,
                insulin.CreatedAt,
                insulin.Note?.Text,
                insulin.InsulinType,
                insulin.Dose.Units,
                insulin.Preparation,
                insulin.Delivery,
                insulin.Timing),

            ExerciseEvent exercise => new ExerciseEventDto(
                exercise.Id,
                userId.Value,
                exercise.EventType,
                exercise.EventTime,
                exercise.CreatedAt,
                exercise.Note?.Text,
                exercise.ExerciseType.Value,
                exercise.Duration.Minutes,
                exercise.Intensity),

            NoteEvent note => new NoteEventDto(
                note.Id,
                userId.Value,
                note.EventType,
                note.EventTime,
                note.CreatedAt,
                null,
                note.Text.Text),

            _ => throw new InvalidOperationException($"Unknown event type: {@event.GetType().Name}")
        };

        return Result.Success(dto);
    }
}

