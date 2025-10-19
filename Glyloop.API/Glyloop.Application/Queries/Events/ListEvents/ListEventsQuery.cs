using Glyloop.Application.DTOs.Common;
using Glyloop.Application.DTOs.Events;
using Glyloop.Domain.Common;
using Glyloop.Domain.Enums;
using MediatR;

namespace Glyloop.Application.Queries.Events.ListEvents;

/// <summary>
/// Query to retrieve a paginated list of events for the current user.
/// Supports filtering by event type and date range.
/// </summary>
public record ListEventsQuery(
    EventType? EventType,
    DateTimeOffset? FromDate,
    DateTimeOffset? ToDate,
    int Page = 1,
    int PageSize = 20) : IRequest<Result<PagedResult<EventListItemDto>>>;

