using Glyloop.Domain.Common;
using MediatR;

namespace Glyloop.Application.Commands.DexcomLink.UnlinkDexcom;

/// <summary>
/// Command to unlink a Dexcom account.
/// Removes the OAuth connection and optionally purges associated data.
/// </summary>
public record UnlinkDexcomCommand(
    Guid LinkId,
    bool PurgeData = false) : IRequest<Result>;

