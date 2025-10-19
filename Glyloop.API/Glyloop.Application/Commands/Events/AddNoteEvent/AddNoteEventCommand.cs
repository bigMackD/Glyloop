using Glyloop.Application.DTOs.Events;
using Glyloop.Domain.Common;
using MediatR;

namespace Glyloop.Application.Commands.Events.AddNoteEvent;

/// <summary>
/// Command to create a standalone note event.
/// Used for general observations, symptoms, or context not tied to specific actions.
/// </summary>
public record AddNoteEventCommand(
    DateTimeOffset EventTime,
    string Text) : IRequest<Result<NoteEventDto>>;

