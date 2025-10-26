using Glyloop.Application.DTOs.DexcomLink;
using Glyloop.Domain.Common;
using MediatR;

namespace Glyloop.Application.Commands.DexcomLink.LinkDexcom;

/// <summary>
/// Command to link a Dexcom account via OAuth authorization.
/// Exchanges authorization code for access/refresh tokens and creates DexcomLink aggregate.
/// </summary>
public record LinkDexcomCommand(
    string AuthorizationCode) : IRequest<Result<DexcomLinkCreatedDto>>;

