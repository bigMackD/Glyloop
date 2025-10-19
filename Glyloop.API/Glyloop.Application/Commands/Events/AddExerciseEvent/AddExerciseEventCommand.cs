using Glyloop.Application.DTOs.Events;
using Glyloop.Domain.Common;
using Glyloop.Domain.Enums;
using MediatR;

namespace Glyloop.Application.Commands.Events.AddExerciseEvent;

/// <summary>
/// Command to create an exercise/physical activity event.
/// Records exercise sessions with type, duration, and intensity information.
/// </summary>
public record AddExerciseEventCommand(
    DateTimeOffset EventTime,
    int ExerciseTypeId,
    int DurationMinutes,
    IntensityType? Intensity,
    string? Note) : IRequest<Result<ExerciseEventDto>>;

