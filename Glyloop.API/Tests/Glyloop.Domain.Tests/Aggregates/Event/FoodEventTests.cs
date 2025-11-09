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
public class FoodEventTests
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

        var result = FoodEvent.Create(
            user,
            now.AddMinutes(1),
            Carbohydrate.Create(10).Value,
            MealTagId.Create(1),
            AbsorptionHint.Normal,
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
        var when = now; // exactly now should be allowed
        var carbs = Carbohydrate.Create(45).Value;
        var tag = MealTagId.Create(3);
        var note = NoteText.Create("  lunch  ").Value;
        var corr = Guid.NewGuid();
        var caus = Guid.NewGuid();

        var result = FoodEvent.Create(
            user, when, carbs, tag, AbsorptionHint.Rapid, note, SourceType.Manual, clock, corr, caus);

        Assert.That(result.IsSuccess, Is.True);

        var e = result.Value;
        Assert.Multiple(() =>
        {
            Assert.That(e.UserId, Is.EqualTo(user));
            Assert.That(e.EventTime, Is.EqualTo(when));
            Assert.That(e.EventType, Is.EqualTo(EventType.Food));
            Assert.That(e.Source, Is.EqualTo(SourceType.Manual));
            Assert.That(e.CreatedAt, Is.EqualTo(now));
            Assert.That(e.Carbohydrates, Is.EqualTo(carbs));
            Assert.That(e.MealTag, Is.EqualTo(tag));
            Assert.That(e.AbsorptionHint, Is.EqualTo(AbsorptionHint.Rapid));
            Assert.That(e.Note, Is.EqualTo(note));
            Assert.That(e.DomainEvents.Count, Is.EqualTo(1));
        });

        var evt = e.DomainEvents.Single() as FoodEventCreatedEvent;
        Assert.That(evt, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(evt!.EventId, Is.EqualTo(e.Id));
            Assert.That(evt!.UserId, Is.EqualTo((Guid)user));
            Assert.That(evt!.EventTime, Is.EqualTo(when));
            Assert.That(evt!.Carbohydrates, Is.EqualTo(carbs.Grams));
            Assert.That(evt!.MealTagId, Is.EqualTo(tag.Value));
            Assert.That(evt!.AbsorptionHint, Is.EqualTo(AbsorptionHint.Rapid));
            Assert.That(evt!.Note, Is.EqualTo(note.Text));
            Assert.That(evt!.OccurredAt, Is.EqualTo(now));
            Assert.That(evt!.CorrelationId, Is.EqualTo(corr));
            Assert.That(evt!.CausationId, Is.EqualTo(caus));
        });
    }
}


