using Glyloop.Application.DTOs.DexcomLink;
using Glyloop.Domain.Common;
using MediatR;

namespace Glyloop.Application.Queries.DexcomLink.GetDexcomLinkStatus;

/// <summary>
/// Query to retrieve the current user's Dexcom link status.
/// Returns whether user has an active link and token expiration information.
/// </summary>
public record GetDexcomLinkStatusQuery : IRequest<Result<DexcomLinkStatusDto>>;

