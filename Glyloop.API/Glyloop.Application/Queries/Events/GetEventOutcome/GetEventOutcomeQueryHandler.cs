using Glyloop.Application.Common.Interfaces;
using Glyloop.Application.DTOs.Events;
using Glyloop.Domain.Aggregates.Event;
using Glyloop.Domain.Common;
using Glyloop.Domain.Enums;
using Glyloop.Domain.Repositories;
using Glyloop.Domain.ValueObjects;
using MediatR;

namespace Glyloop.Application.Queries.Events.GetEventOutcome;

/// <summary>
/// Handler for GetEventOutcomeQuery.
/// Retrieves glucose reading ~2 hours after a food event to analyze impact.
/// </summary>
public class GetEventOutcomeQueryHandler : IRequestHandler<GetEventOutcomeQuery, Result<EventOutcomeDto>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IGlucoseReadingService _glucoseReadingService;
    private readonly ICurrentUserService _currentUserService;

    public GetEventOutcomeQueryHandler(
        IEventRepository eventRepository,
        IGlucoseReadingService glucoseReadingService,
        ICurrentUserService currentUserService)
    {
        _eventRepository = eventRepository;
        _glucoseReadingService = glucoseReadingService;
        _currentUserService = currentUserService;
    }

    public async Task<Result<EventOutcomeDto>> Handle(
        GetEventOutcomeQuery request,
        CancellationToken cancellationToken)
    {
        var userId = UserId.Create(_currentUserService.UserId);

        var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        
        if (@event == null)
        {
            return Result.Failure<EventOutcomeDto>(
                Error.Create("Event.NotFound", "Event not found."));
        }

        if (@event.UserId.Value != userId.Value)
        {
            return Result.Failure<EventOutcomeDto>(
                Error.Create("Authorization.Forbidden", "User does not own this event."));
        }

        if (@event is not FoodEvent)
        {
            return Result.Failure<EventOutcomeDto>(
                Error.Create("Event.InvalidType", "Event outcome is only available for food events."));
        }

        var targetTime = @event.EventTime.AddHours(2);

        var readingResult = await _glucoseReadingService.GetReadingNearTimeAsync(
            userId,
            targetTime,
            toleranceMinutes: 15,
            cancellationToken);

        if (readingResult.IsFailure)
        {
            return Result.Failure<EventOutcomeDto>(readingResult.Error);
        }

        var reading = readingResult.Value;

        var dto = new EventOutcomeDto(
            @event.Id,
            targetTime,
            reading?.ValueMgDl,
            reading?.SystemTime,
            reading != null);

        return Result.Success(dto);
    }
}

