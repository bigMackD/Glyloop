using Glyloop.Application.DTOs.Events;
using Glyloop.Domain.Common;
using MediatR;

namespace Glyloop.Application.Queries.Events.GetEventOutcome;

/// <summary>
/// Query to retrieve glucose outcome for a food event.
/// Returns glucose reading approximately 2 hours after the event.
/// </summary>
public record GetEventOutcomeQuery(
    Guid EventId) : IRequest<Result<EventOutcomeDto>>;

