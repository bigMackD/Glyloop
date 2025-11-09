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
public class InsulinEventTests
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

        var result = InsulinEvent.Create(
            user,
            now.AddSeconds(1),
            InsulinType.Fast,
            InsulinDose.Create(2).Value,
            null,
            null,
            null,
            null,
            SourceType.Manual,
            clock,
            Guid.NewGuid(),
            Guid.NewGuid());

        Assert.That(result.IsFailure, Is.True);
    }

    [Test]
    public void Create_ShouldSucceed_AndRaiseEvent_WithFlattenedFields()
    {
        var now = new DateTimeOffset(2025, 11, 8, 12, 0, 0, TimeSpan.Zero);
        var clock = Clock(now);
        var user = UserId.Create(Guid.NewGuid());
        var when = now;
        var dose = InsulinDose.Create(3.5m).Value;
        var prep = "Brand X";
        var deliv = "Left arm";
        var timing = "Before meal";
        var note = NoteText.Create("bolus").Value;
        var corr = Guid.NewGuid();
        var caus = Guid.NewGuid();

        var result = InsulinEvent.Create(
            user, when, InsulinType.Fast, dose, prep, deliv, timing, note, SourceType.Manual, clock, corr, caus);

        Assert.That(result.IsSuccess, Is.True);

        var e = result.Value;
        Assert.Multiple(() =>
        {
            Assert.That(e.UserId, Is.EqualTo(user));
            Assert.That(e.EventTime, Is.EqualTo(when));
            Assert.That(e.EventType, Is.EqualTo(EventType.Insulin));
            Assert.That(e.Source, Is.EqualTo(SourceType.Manual));
            Assert.That(e.CreatedAt, Is.EqualTo(now));
            Assert.That(e.InsulinType, Is.EqualTo(InsulinType.Fast));
            Assert.That(e.Dose, Is.EqualTo(dose));
            Assert.That(e.Preparation, Is.EqualTo(prep));
            Assert.That(e.Delivery, Is.EqualTo(deliv));
            Assert.That(e.Timing, Is.EqualTo(timing));
            Assert.That(e.Note!.Text, Is.EqualTo("bolus"));
            Assert.That(e.DomainEvents.Count, Is.EqualTo(1));
        });

        var evt = e.DomainEvents.Single() as InsulinEventCreatedEvent;
        Assert.That(evt, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(evt!.EventId, Is.EqualTo(e.Id));
            Assert.That(evt!.UserId, Is.EqualTo((Guid)user));
            Assert.That(evt!.EventTime, Is.EqualTo(when));
            Assert.That(evt!.InsulinType, Is.EqualTo(InsulinType.Fast));
            Assert.That(evt!.Dose, Is.EqualTo(dose.Units));
            Assert.That(evt!.Preparation, Is.EqualTo(prep));
            Assert.That(evt!.Delivery, Is.EqualTo(deliv));
            Assert.That(evt!.Timing, Is.EqualTo(timing));
            Assert.That(evt!.Note, Is.EqualTo(note.Text));
            Assert.That(evt!.OccurredAt, Is.EqualTo(now));
            Assert.That(evt!.CorrelationId, Is.EqualTo(corr));
            Assert.That(evt!.CausationId, Is.EqualTo(caus));
        });
    }
}


