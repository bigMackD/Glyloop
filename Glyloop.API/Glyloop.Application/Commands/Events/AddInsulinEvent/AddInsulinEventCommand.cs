using Glyloop.Application.DTOs.Events;
using Glyloop.Domain.Common;
using Glyloop.Domain.Enums;
using MediatR;

namespace Glyloop.Application.Commands.Events.AddInsulinEvent;

/// <summary>
/// Command to create an insulin administration event.
/// Records insulin doses for both fast-acting and long-acting insulin types.
/// </summary>
public record AddInsulinEventCommand(
    DateTimeOffset EventTime,
    InsulinType InsulinType,
    decimal Units,
    string? Preparation,
    string? Delivery,
    string? Timing,
    string? Note) : IRequest<Result<InsulinEventDto>>;

