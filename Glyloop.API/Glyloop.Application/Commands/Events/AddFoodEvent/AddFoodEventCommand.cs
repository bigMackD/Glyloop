using Glyloop.Application.DTOs.Events;
using Glyloop.Domain.Common;
using Glyloop.Domain.Enums;
using MediatR;

namespace Glyloop.Application.Commands.Events.AddFoodEvent;

/// <summary>
/// Command to create a food intake event.
/// Records carbohydrate consumption with meal context and absorption information.
/// </summary>
public record AddFoodEventCommand(
    DateTimeOffset EventTime,
    int CarbohydratesGrams,
    int? MealTagId,
    AbsorptionHint? AbsorptionHint,
    string? Note) : IRequest<Result<FoodEventDto>>;

