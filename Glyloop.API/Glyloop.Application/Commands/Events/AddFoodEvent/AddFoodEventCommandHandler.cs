using Glyloop.Application.Common.Interfaces;
using Glyloop.Application.DTOs.Events;
using Glyloop.Domain.Aggregates.Event;
using Glyloop.Domain.Common;
using Glyloop.Domain.Enums;
using Glyloop.Domain.Repositories;
using Glyloop.Domain.ValueObjects;
using MediatR;

namespace Glyloop.Application.Commands.Events.AddFoodEvent;

/// <summary>
/// Handler for AddFoodEventCommand.
/// Creates a food event aggregate and persists it to the repository.
/// </summary>
public class AddFoodEventCommandHandler : IRequestHandler<AddFoodEventCommand, Result<FoodEventDto>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITimeProvider _timeProvider;

    public AddFoodEventCommandHandler(
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

    public async Task<Result<FoodEventDto>> Handle(
        AddFoodEventCommand request,
        CancellationToken cancellationToken)
    {
        var userId = UserId.Create(_currentUserService.UserId);

        var carbohydratesResult = Carbohydrate.Create(request.CarbohydratesGrams);
        if (carbohydratesResult.IsFailure)
        {
            return Result.Failure<FoodEventDto>(carbohydratesResult.Error);
        }

        var mealTagId = MealTagId.Create(request.MealTagId ?? 1);

        var absorptionHint = request.AbsorptionHint ?? Domain.Enums.AbsorptionHint.Normal;

        var note = NoteText.CreateOptional(request.Note);

        var correlationId = Guid.NewGuid();
        var causationId = Guid.NewGuid();

        var foodEventResult = FoodEvent.Create(
            userId,
            request.EventTime,
            carbohydratesResult.Value,
            mealTagId,
            absorptionHint,
            note,
            SourceType.Manual,
            _timeProvider,
            correlationId,
            causationId);

        if (foodEventResult.IsFailure)
        {
            return Result.Failure<FoodEventDto>(foodEventResult.Error);
        }

        var foodEvent = foodEventResult.Value;

        _eventRepository.Add(foodEvent);

        await _unitOfWork.SaveEntitiesAsync(cancellationToken);

        var dto = new FoodEventDto(
            foodEvent.Id,
            userId.Value,
            foodEvent.EventType,
            foodEvent.EventTime,
            foodEvent.CreatedAt,
            foodEvent.Note?.Text,
            foodEvent.Carbohydrates.Grams,
            foodEvent.MealTag.Value,
            foodEvent.AbsorptionHint);

        return Result.Success(dto);
    }
}

