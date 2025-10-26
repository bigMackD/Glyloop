using Glyloop.Application.DTOs.Account;
using Glyloop.Domain.Common;
using MediatR;

namespace Glyloop.Application.Queries.Account.GetUserPreferences;

/// <summary>
/// Query to retrieve the current user's TIR range preferences.
/// </summary>
public record GetUserPreferencesQuery : IRequest<Result<UserPreferencesDto>>;

