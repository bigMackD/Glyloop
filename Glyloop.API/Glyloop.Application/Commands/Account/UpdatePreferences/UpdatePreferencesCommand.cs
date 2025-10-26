using Glyloop.Application.DTOs.Account;
using Glyloop.Domain.Common;
using MediatR;

namespace Glyloop.Application.Commands.Account.UpdatePreferences;

/// <summary>
/// Command to update user's Time in Range preferences.
/// Updates the glucose range bounds used for TIR calculations.
/// </summary>
public record UpdatePreferencesCommand(
    int LowerBound,
    int UpperBound) : IRequest<Result<UserPreferencesDto>>;

