using Glyloop.Application.DTOs.Events;
using Glyloop.Domain.Common;
using MediatR;

namespace Glyloop.Application.Queries.Events.GetEventById;

/// <summary>
/// Query to retrieve detailed event information by ID.
/// Returns the appropriate event DTO based on the event type.
/// </summary>
public record GetEventByIdQuery(
    Guid EventId) : IRequest<Result<EventDto>>;

