using Glyloop.Application.Common.Interfaces;
using Glyloop.Application.DTOs.Chart;
using Glyloop.Domain.Aggregates.Event;
using Glyloop.Domain.Common;
using Glyloop.Domain.Enums;
using Glyloop.Domain.Repositories;
using Glyloop.Domain.ValueObjects;
using MediatR;

namespace Glyloop.Application.Queries.Chart.GetChartData;

/// <summary>
/// Handler for GetChartDataQuery.
/// Retrieves glucose readings and events for chart visualization.
/// </summary>
public class GetChartDataQueryHandler : IRequestHandler<GetChartDataQuery, Result<ChartDataDto>>
{
    private readonly IGlucoseReadingService _glucoseReadingService;
    private readonly IEventRepository _eventRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITimeProvider _timeProvider;

    private static readonly Dictionary<string, int> RangeHours = new()
    {
        ["1h"] = 1,
        ["3h"] = 3,
        ["6h"] = 6,
        ["12h"] = 12,
        ["24h"] = 24
    };

    public GetChartDataQueryHandler(
        IGlucoseReadingService glucoseReadingService,
        IEventRepository eventRepository,
        ICurrentUserService currentUserService,
        ITimeProvider timeProvider)
    {
        _glucoseReadingService = glucoseReadingService;
        _eventRepository = eventRepository;
        _currentUserService = currentUserService;
        _timeProvider = timeProvider;
    }

    public async Task<Result<ChartDataDto>> Handle(
        GetChartDataQuery request,
        CancellationToken cancellationToken)
    {
        var userId = UserId.Create(_currentUserService.UserId);

        // Parse range
        if (!RangeHours.TryGetValue(request.Range.ToLowerInvariant(), out var hours))
        {
            return Result.Failure<ChartDataDto>(
                Error.Create("Chart.InvalidRange", "Range must be one of: 1h, 3h, 6h, 12h, 24h."));
        }

        var endTime = _timeProvider.UtcNow;
        var startTime = endTime.AddHours(-hours);

        var glucoseTask = _glucoseReadingService.GetReadingsInRangeAsync(
            userId, startTime, endTime, cancellationToken);
        
        var eventsTask = _eventRepository.GetByUserIdAsync(
            userId, null, startTime, endTime, cancellationToken);

        await Task.WhenAll(glucoseTask, eventsTask);

        var glucoseResult = await glucoseTask;
        if (glucoseResult.IsFailure)
        {
            return Result.Failure<ChartDataDto>(glucoseResult.Error);
        }

        var events = await eventsTask;

        var glucoseData = glucoseResult.Value
            .Select(r => new GlucoseDataPointDto(r.SystemTime, r.ValueMgDl, r.Trend))
            .ToList();

        var eventOverlays = events
            .Select(e => new EventOverlayDto(
                e.Id,
                e.EventType,
                e.EventTime,
                CreateEventTooltip(e)))
            .ToList();

        var dto = new ChartDataDto(
            startTime,
            endTime,
            glucoseData,
            eventOverlays);

        return Result.Success(dto);
    }

    private static string CreateEventTooltip(Event @event) => @event switch
    {
        FoodEvent food => $"{food.Carbohydrates.Grams}g carbs",
        InsulinEvent insulin => $"{insulin.Dose.Units}U {insulin.InsulinType}",
        ExerciseEvent exercise => $"{exercise.Duration.Minutes}min exercise",
        NoteEvent note => note.Text.Text.Length > 30 
            ? $"{note.Text.Text[..27]}..." 
            : note.Text.Text,
        _ => "Event"
    };
}

