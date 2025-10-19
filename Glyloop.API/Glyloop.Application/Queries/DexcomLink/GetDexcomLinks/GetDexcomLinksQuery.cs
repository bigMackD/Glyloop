using Glyloop.Application.DTOs.DexcomLink;
using Glyloop.Domain.Common;
using MediatR;

namespace Glyloop.Application.Queries.DexcomLink.GetDexcomLinks;

/// <summary>
/// Query to retrieve all Dexcom links for the current user.
/// Returns both active and historical links.
/// </summary>
public record GetDexcomLinksQuery : IRequest<Result<List<DexcomLinkDto>>>;

