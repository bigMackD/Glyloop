using Glyloop.Domain.Aggregates.DexcomLink.Events;
using Glyloop.Domain.Common;
using Glyloop.Domain.Errors;
using Glyloop.Domain.ValueObjects;
using NSubstitute;
using NUnit.Framework;
using DomainDexcomLink = Glyloop.Domain.Aggregates.DexcomLink.DexcomLink;

namespace Glyloop.Domain.Tests.Aggregates.DexcomLink;

[TestFixture]
[Parallelizable(ParallelScope.All)]
[Category("Unit")]
public class DexcomLinkTests
{
    private static ITimeProvider CreateClock(DateTimeOffset now)
    {
        var clock = Substitute.For<ITimeProvider>();
        clock.UtcNow.Returns(now);
        return clock;
    }

    [Test]
    public void Create_ShouldFail_WhenTokensEmpty()
    {
        var userId = UserId.Create(Guid.NewGuid());
        var now = DateTimeOffset.UtcNow;
        var clock = CreateClock(now);
        var expires = now.AddHours(2);
        var corr = Guid.NewGuid();
        var caus = Guid.NewGuid();

        var empty = Array.Empty<byte>();

        var r1 = DomainDexcomLink.Create(userId, empty, new byte[] { 1 }, expires, clock, corr, caus);
        var r2 = DomainDexcomLink.Create(userId, new byte[] { 1 }, empty, expires, clock, corr, caus);

        Assert.Multiple(() =>
        {
            Assert.That(r1.IsFailure, Is.True);
            Assert.That(r2.IsFailure, Is.True);
        });
    }

    [Test]
    public void Create_ShouldFail_WhenExpired()
    {
        var userId = UserId.Create(Guid.NewGuid());
        var now = DateTimeOffset.UtcNow;
        var clock = CreateClock(now);
        var corr = Guid.NewGuid();
        var caus = Guid.NewGuid();

        var result = DomainDexcomLink.Create(userId, new byte[] { 1 }, new byte[] { 2 }, now, clock, corr, caus);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error, Is.EqualTo(DomainErrors.DexcomLink.TokenExpired));
        });
    }

    [Test]
    public void Create_ShouldSucceed_AndRaiseEvent()
    {
        var userId = UserId.Create(Guid.NewGuid());
        var now = new DateTimeOffset(2025, 11, 8, 10, 0, 0, TimeSpan.Zero);
        var clock = CreateClock(now);
        var expires = now.AddHours(4);
        var corr = Guid.NewGuid();
        var caus = Guid.NewGuid();

        var result = DomainDexcomLink.Create(userId, new byte[] { 10 }, new byte[] { 20 }, expires, clock, corr, caus);
        Assert.That(result.IsSuccess, Is.True);

        var link = result.Value;

        Assert.Multiple(() =>
        {
            Assert.That(link.UserId, Is.EqualTo(userId));
            Assert.That(link.TokenExpiresAt, Is.EqualTo(expires));
            Assert.That(link.LastRefreshedAt, Is.EqualTo(now));
            Assert.That(link.DomainEvents.Count, Is.EqualTo(1));
        });

        var evt = link.DomainEvents.Single() as DexcomLinkedEvent;
        Assert.That(evt, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(evt!.UserId, Is.EqualTo((Guid)userId));
            Assert.That(evt!.LinkId, Is.EqualTo(link.Id));
            Assert.That(evt!.OccurredAt, Is.EqualTo(now));
            Assert.That(evt!.CorrelationId, Is.EqualTo(corr));
            Assert.That(evt!.CausationId, Is.EqualTo(caus));
        });
    }

    [Test]
    public void RefreshTokens_ShouldFail_WhenInvalidInputs()
    {
        var userId = UserId.Create(Guid.NewGuid());
        var now = new DateTimeOffset(2025, 11, 8, 10, 0, 0, TimeSpan.Zero);
        var clock = CreateClock(now);
        var corr = Guid.NewGuid();
        var caus = Guid.NewGuid();

        var link = DomainDexcomLink.Create(userId, new byte[] { 1 }, new byte[] { 2 }, now.AddHours(1), clock, corr, caus).Value;

        var r1 = link.RefreshTokens(Array.Empty<byte>(), new byte[] { 9 }, now.AddHours(2), clock, corr, caus);
        var r2 = link.RefreshTokens(new byte[] { 9 }, Array.Empty<byte>(), now.AddHours(2), clock, corr, caus);
        var r3 = link.RefreshTokens(new byte[] { 9 }, new byte[] { 9 }, now, clock, corr, caus);

        Assert.Multiple(() =>
        {
            Assert.That(r1.IsFailure, Is.True);
            Assert.That(r2.IsFailure, Is.True);
            Assert.That(r3.IsFailure, Is.True);
            Assert.That(r3.Error, Is.EqualTo(DomainErrors.DexcomLink.TokenExpired));
        });
    }

    [Test]
    public void RefreshTokens_ShouldUpdateState_AndRaiseEvent()
    {
        var userId = UserId.Create(Guid.NewGuid());
        var now = new DateTimeOffset(2025, 11, 8, 10, 0, 0, TimeSpan.Zero);
        var later = now.AddMinutes(5);
        var clock1 = CreateClock(now);
        var corr = Guid.NewGuid();
        var caus = Guid.NewGuid();

        var link = DomainDexcomLink.Create(userId, new byte[] { 1 }, new byte[] { 2 }, now.AddHours(1), clock1, corr, caus).Value;

        var clock2 = CreateClock(later);
        var newExpires = later.AddHours(2);
        var result = link.RefreshTokens(new byte[] { 3 }, new byte[] { 4 }, newExpires, clock2, corr, caus);
        Assert.That(result.IsSuccess, Is.True);

        Assert.Multiple(() =>
        {
            Assert.That(link.EncryptedAccessToken, Is.EqualTo(new byte[] { 3 }));
            Assert.That(link.EncryptedRefreshToken, Is.EqualTo(new byte[] { 4 }));
            Assert.That(link.TokenExpiresAt, Is.EqualTo(newExpires));
            Assert.That(link.LastRefreshedAt, Is.EqualTo(later));
            Assert.That(link.DomainEvents.Count, Is.EqualTo(2)); // linked + refreshed
        });

        var refreshedEvt = link.DomainEvents.Last() as DexcomTokensRefreshedEvent;
        Assert.That(refreshedEvt, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(refreshedEvt!.LinkId, Is.EqualTo(link.Id));
            Assert.That(refreshedEvt!.NewExpiresAt, Is.EqualTo(newExpires));
            Assert.That(refreshedEvt!.OccurredAt, Is.EqualTo(later));
        });
    }

    [Test]
    public void IsActive_And_ShouldRefresh_ShouldReflectExpiry()
    {
        var userId = UserId.Create(Guid.NewGuid());
        var now = DateTimeOffset.UtcNow;
        var clock = CreateClock(now);
        var link = DomainDexcomLink.Create(userId, new byte[] { 1 }, new byte[] { 2 }, now.AddHours(2), clock, Guid.NewGuid(), Guid.NewGuid()).Value;

        Assert.Multiple(() =>
        {
            Assert.That(link.IsActive, Is.True);
            Assert.That(link.ShouldRefresh, Is.False);
        });

        // Move expiry within the next hour to trigger ShouldRefresh; uses system UtcNow
        link.RefreshTokens(new byte[] { 5 }, new byte[] { 6 }, DateTimeOffset.UtcNow.AddMinutes(30), clock, Guid.NewGuid(), Guid.NewGuid());

        Assert.That(link.ShouldRefresh, Is.True);
    }

    [Test]
    public void Unlink_ShouldRaiseEvent_WithPurgeFlag()
    {
        var userId = UserId.Create(Guid.NewGuid());
        var now = new DateTimeOffset(2025, 11, 8, 10, 0, 0, TimeSpan.Zero);
        var clock = CreateClock(now);
        var link = DomainDexcomLink.Create(userId, new byte[] { 1 }, new byte[] { 2 }, now.AddHours(2), clock, Guid.NewGuid(), Guid.NewGuid()).Value;

        var corr = Guid.NewGuid();
        var caus = Guid.NewGuid();
        link.Unlink(true, corr, caus);

        var evt = link.DomainEvents.Last() as DexcomUnlinkedEvent;
        Assert.That(evt, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(evt!.UserId, Is.EqualTo((Guid)userId));
            Assert.That(evt!.LinkId, Is.EqualTo(link.Id));
            Assert.That(evt!.DataPurged, Is.True);
            Assert.That(evt!.CorrelationId, Is.EqualTo(corr));
            Assert.That(evt!.CausationId, Is.EqualTo(caus));
        });
    }
}


