using Glyloop.Application.Common.Interfaces;
using Glyloop.Application.DTOs.Common;
using Glyloop.Application.DTOs.Events;
using Glyloop.Domain.Aggregates.Event;
using Glyloop.Domain.Common;
using Glyloop.Domain.Repositories;
using Glyloop.Domain.ValueObjects;
using MediatR;

namespace Glyloop.Application.Queries.Events.ListEvents;

/// <summary>
/// Handler for ListEventsQuery.
/// Retrieves paginated event history with optional filters.
/// </summary>
public class ListEventsQueryHandler : IRequestHandler<ListEventsQuery, Result<PagedResult<EventListItemDto>>>
{
    private readonly IEventRepository _eventRepository;
    private readonly ICurrentUserService _currentUserService;

    public ListEventsQueryHandler(
        IEventRepository eventRepository,
        ICurrentUserService currentUserService)
    {
        _eventRepository = eventRepository;
        _currentUserService = currentUserService;
    }

    public async Task<Result<PagedResult<EventListItemDto>>> Handle(
        ListEventsQuery request,
        CancellationToken cancellationToken)
    {
        // Get current user ID (guaranteed by API layer authentication)
        var userId = UserId.Create(_currentUserService.UserId);

        // Set default date range if not provided (last 30 days)
        var toDate = request.ToDate ?? DateTimeOffset.UtcNow;
        var fromDate = request.FromDate ?? toDate.AddDays(-30);

        // Get total count for pagination
        var totalCount = await _eventRepository.CountByUserIdAsync(
            userId,
            fromDate,
            toDate,
            request.EventType,
            cancellationToken);

        // Get paginated events
        var events = await _eventRepository.GetPagedAsync(
            userId,
            fromDate,
            toDate,
            request.Page,
            request.PageSize,
            cancellationToken);

        // Map to DTOs with summaries
        var items = events.Select(e => new EventListItemDto(
            e.Id,
            e.EventType,
            e.EventTime,
            CreateSummary(e)))
            .ToList();

        var pagedResult = new PagedResult<EventListItemDto>(
            items,
            totalCount,
            request.Page,
            request.PageSize);

        return Result.Success(pagedResult);
    }

    /// <summary>
    /// Creates a human-readable summary for an event.
    /// </summary>
    private static string CreateSummary(Event @event)
    {
        return @event switch
        {
            FoodEvent food => $"{food.Carbohydrates.Grams}g carbs",
            InsulinEvent insulin => $"{insulin.Dose.Units}U {insulin.InsulinType}",
            ExerciseEvent exercise => $"{exercise.Duration.Minutes}min",
            NoteEvent note => note.Text.Text.Length > 50
                ? $"{note.Text.Text[..47]}..."
                : note.Text.Text,
            _ => "Unknown event"
        };
    }
}

