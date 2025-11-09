using Glyloop.Application.Common.Interfaces;
using Glyloop.Domain.Common;
using Glyloop.Infrastructure.Services.Dexcom;
using Glyloop.Infrastructure.Services.Dexcom.Models;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;

namespace Glyloop.Infrastructure.Tests.Services.Dexcom;

[TestFixture]
[Parallelizable(ParallelScope.All)]
[Category("Unit")]
public class DexcomServiceTests
{
    [Test]
    public async Task ExchangeCodeForTokensAsync_ShouldMapSuccess()
    {
        var api = Substitute.For<IDexcomApiClient>();
        var logger = Substitute.For<ILogger<DexcomService>>();
        var svc = new DexcomService(api, logger);

        api.ExchangeCodeForTokensAsync("code", Arg.Any<CancellationToken>())
            .Returns(Result.Success(new OAuthTokenResponse("acc", "ref", 3600, "Bearer")));

        var result = await svc.ExchangeCodeForTokensAsync("code");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.AccessToken, Is.EqualTo("acc"));
            Assert.That(result.Value.RefreshToken, Is.EqualTo("ref"));
            Assert.That(result.Value.ExpiresInSeconds, Is.EqualTo(3600));
        });
    }

    [Test]
    public async Task ExchangeCodeForTokensAsync_ShouldPropagateFailure()
    {
        var api = Substitute.For<IDexcomApiClient>();
        var logger = Substitute.For<ILogger<DexcomService>>();
        var svc = new DexcomService(api, logger);

        var err = Error.Create("X", "Y");
        api.ExchangeCodeForTokensAsync("code", Arg.Any<CancellationToken>())
            .Returns(Result.Failure<OAuthTokenResponse>(err));

        var result = await svc.ExchangeCodeForTokensAsync("code");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error, Is.EqualTo(err));
        });
    }

    [Test]
    public async Task RefreshTokenAsync_ShouldMapSuccess()
    {
        var api = Substitute.For<IDexcomApiClient>();
        var logger = Substitute.For<ILogger<DexcomService>>();
        var svc = new DexcomService(api, logger);

        api.RefreshTokenAsync("r", Arg.Any<CancellationToken>())
            .Returns(Result.Success(new OAuthTokenResponse("acc2", "ref2", 7200, "Bearer")));

        var result = await svc.RefreshTokenAsync("r");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.RefreshToken, Is.EqualTo("ref2"));
        });
    }
}



