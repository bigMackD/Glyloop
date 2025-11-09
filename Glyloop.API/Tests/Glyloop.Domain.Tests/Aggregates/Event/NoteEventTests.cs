using Glyloop.Domain.Aggregates.Event;
using Glyloop.Domain.Aggregates.Event.Events;
using Glyloop.Domain.Common;
using Glyloop.Domain.Enums;
using Glyloop.Domain.ValueObjects;
using NSubstitute;
using NUnit.Framework;

namespace Glyloop.Domain.Tests.Aggregates.Event;

[TestFixture]
[Parallelizable(ParallelScope.All)]
[Category("Unit")]
public class NoteEventTests
{
    private static ITimeProvider Clock(DateTimeOffset now)
    {
        var clock = Substitute.For<ITimeProvider>();
        clock.UtcNow.Returns(now);
        return clock;
    }

    [Test]
    public void Create_ShouldFail_WhenInFuture()
    {
        var now = new DateTimeOffset(2025, 11, 8, 12, 0, 0, TimeSpan.Zero);
        var clock = Clock(now);
        var user = UserId.Create(Guid.NewGuid());

        var result = NoteEvent.Create(
            user,
            now.AddMinutes(1),
            NoteText.Create("note").Value,
            SourceType.Manual,
            clock,
            Guid.NewGuid(),
            Guid.NewGuid());

        Assert.That(result.IsFailure, Is.True);
    }

    [Test]
    public void Create_ShouldSucceed_AndRaiseEvent()
    {
        var now = new DateTimeOffset(2025, 11, 8, 12, 0, 0, TimeSpan.Zero);
        var clock = Clock(now);
        var user = UserId.Create(Guid.NewGuid());
        var when = now;
        var text = NoteText.Create("  hello ").Value;
        var corr = Guid.NewGuid();
        var caus = Guid.NewGuid();

        var result = NoteEvent.Create(user, when, text, SourceType.Manual, clock, corr, caus);
        Assert.That(result.IsSuccess, Is.True);

        var e = result.Value;
        Assert.Multiple(() =>
        {
            Assert.That(e.UserId, Is.EqualTo(user));
            Assert.That(e.EventTime, Is.EqualTo(when));
            Assert.That(e.EventType, Is.EqualTo(EventType.Note));
            Assert.That(e.Source, Is.EqualTo(SourceType.Manual));
            Assert.That(e.CreatedAt, Is.EqualTo(now));
            Assert.That(e.Text, Is.EqualTo(text));
            Assert.That(e.Note, Is.Null); // base Note is null for NoteEvent
            Assert.That(e.DomainEvents.Count, Is.EqualTo(1));
        });

        var evt = e.DomainEvents.Single() as NoteEventCreatedEvent;
        Assert.That(evt, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(evt!.EventId, Is.EqualTo(e.Id));
            Assert.That(evt!.UserId, Is.EqualTo((Guid)user));
            Assert.That(evt!.EventTime, Is.EqualTo(when));
            Assert.That(evt!.Text, Is.EqualTo(text.Text));
            Assert.That(evt!.OccurredAt, Is.EqualTo(now));
            Assert.That(evt!.CorrelationId, Is.EqualTo(corr));
            Assert.That(evt!.CausationId, Is.EqualTo(caus));
        });
    }
}


