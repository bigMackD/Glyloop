using Glyloop.Application.Common.Interfaces;
using Glyloop.Domain.Aggregates.DexcomLink;
using Glyloop.Domain.Common;
using Glyloop.Domain.Repositories;
using Glyloop.Domain.ValueObjects;
using Glyloop.Infrastructure.Services.Dexcom;
using Glyloop.Infrastructure.Services.Dexcom.Models;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;

namespace Glyloop.Infrastructure.Tests.Services.Dexcom;

[TestFixture]
[Parallelizable(ParallelScope.All)]
[Category("Unit")]
public class GlucoseReadingServiceTests
{
    private static (IDexcomApiClient api, IDexcomLinkRepository repo, ITokenEncryptionService enc, ILogger<GlucoseReadingService> log) Mocks()
    {
        return (Substitute.For<IDexcomApiClient>(),
                Substitute.For<IDexcomLinkRepository>(),
                Substitute.For<ITokenEncryptionService>(),
                Substitute.For<ILogger<GlucoseReadingService>>());
    }

    [Test]
    public async Task GetReadingNearTimeAsync_ShouldReturnNull_WhenNoActiveLink()
    {
        var (api, repo, enc, log) = Mocks();
        repo.GetActiveByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>()).Returns((DexcomLink?)null);

        var svc = new GlucoseReadingService(api, repo, enc, log);
        var result = await svc.GetReadingNearTimeAsync(UserId.Create(Guid.NewGuid()), DateTimeOffset.UtcNow);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Is.Null);
        });
    }

    [Test]
    public async Task GetReadingNearTimeAsync_ShouldPickClosestReading()
    {
        var (api, repo, enc, log) = Mocks();

        // Create a dummy DexcomLink (not used for logic except presence)
        var userId = UserId.Create(Guid.NewGuid());
        var timeProvider = Substitute.For<Glyloop.Domain.Common.ITimeProvider>();
        timeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
        var link = DexcomLink.Create(userId, new byte[] { 1 }, new byte[] { 2 }, DateTimeOffset.UtcNow.AddHours(1), timeProvider, Guid.NewGuid(), Guid.NewGuid()).Value;
        repo.GetActiveByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(link);

        enc.Decrypt(Arg.Any<byte[]>()).Returns("access");

        var target = DateTimeOffset.UtcNow;
        var readings = new GlucoseReadingsResponse(new List<Glyloop.Infrastructure.Services.Dexcom.Models.GlucoseReading>
        {
            new(target.AddMinutes(-10), target.AddMinutes(-10), 100, "mg/dL", "flat"),
            new(target.AddMinutes(-1), target.AddMinutes(-1), 110, "mg/dL", "flat"),
            new(target.AddMinutes(5), target.AddMinutes(5), 115, "mg/dL", "flat")
        });

        api.GetGlucoseReadingsAsync("access", target.AddMinutes(-15), target.AddMinutes(15), Arg.Any<CancellationToken>())
            .Returns(Result.Success(readings));

        var svc = new GlucoseReadingService(api, repo, enc, log);
        var result = await svc.GetReadingNearTimeAsync(userId, target);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Is.Not.Null);
            Assert.That(result.Value!.ValueMgDl, Is.EqualTo(110));
        });
    }

    [Test]
    public async Task GetReadingsInRangeAsync_ShouldReturnEmpty_WhenNoLink()
    {
        var (api, repo, enc, log) = Mocks();
        repo.GetActiveByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>()).Returns((DexcomLink?)null);

        var svc = new GlucoseReadingService(api, repo, enc, log);
        var result = await svc.GetReadingsInRangeAsync(UserId.Create(Guid.NewGuid()), DateTimeOffset.UtcNow.AddHours(-1), DateTimeOffset.UtcNow);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Is.Empty);
        });
    }

    [Test]
    public async Task GetReadingsInRangeAsync_ShouldMapOnSuccess()
    {
        var (api, repo, enc, log) = Mocks();
        var userId = UserId.Create(Guid.NewGuid());
        var timeProvider = Substitute.For<Glyloop.Domain.Common.ITimeProvider>();
        timeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
        var link = DexcomLink.Create(userId, new byte[] { 1 }, new byte[] { 2 }, DateTimeOffset.UtcNow.AddHours(1), timeProvider, Guid.NewGuid(), Guid.NewGuid()).Value;
        repo.GetActiveByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(link);
        enc.Decrypt(Arg.Any<byte[]>()).Returns("access");

        var start = DateTimeOffset.UtcNow.AddHours(-2);
        var end = DateTimeOffset.UtcNow;
        var readings = new GlucoseReadingsResponse(new List<Glyloop.Infrastructure.Services.Dexcom.Models.GlucoseReading>
        {
            new(start.AddMinutes(1), start.AddMinutes(1), 101, "mg/dL", "flat"),
            new(end.AddMinutes(-1), end.AddMinutes(-1), 105, "mg/dL", "flat"),
        });
        api.GetGlucoseReadingsAsync("access", start, end, Arg.Any<CancellationToken>()).Returns(Result.Success(readings));

        var svc = new GlucoseReadingService(api, repo, enc, log);
        var result = await svc.GetReadingsInRangeAsync(userId, start, end);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.Count, Is.EqualTo(2));
            Assert.That(result.Value[0].ValueMgDl, Is.EqualTo(101));
            Assert.That(result.Value[1].ValueMgDl, Is.EqualTo(105));
        });
    }
}


