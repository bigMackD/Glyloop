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
public class ExerciseEventTests
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

        var result = ExerciseEvent.Create(
            user,
            now.AddMinutes(10),
            ExerciseTypeId.Create(1),
            ExerciseDuration.Create(30).Value,
            IntensityType.Moderate,
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
        var type = ExerciseTypeId.Create(2);
        var duration = ExerciseDuration.Create(45).Value;
        var note = NoteText.Create("run").Value;
        var corr = Guid.NewGuid();
        var caus = Guid.NewGuid();

        var result = ExerciseEvent.Create(
            user,
            when,
            type,
            duration,
            IntensityType.Vigorous,
            note,
            SourceType.Manual,
            clock,
            corr,
            caus);

        Assert.That(result.IsSuccess, Is.True);

        var e = result.Value;
        Assert.Multiple(() =>
        {
            Assert.That(e.UserId, Is.EqualTo(user));
            Assert.That(e.EventTime, Is.EqualTo(when));
            Assert.That(e.EventType, Is.EqualTo(EventType.Exercise));
            Assert.That(e.Source, Is.EqualTo(SourceType.Manual));
            Assert.That(e.CreatedAt, Is.EqualTo(now));
            Assert.That(e.ExerciseType, Is.EqualTo(type));
            Assert.That(e.Duration, Is.EqualTo(duration));
            Assert.That(e.Intensity, Is.EqualTo(IntensityType.Vigorous));
            Assert.That(e.Note, Is.EqualTo(note));
            Assert.That(e.DomainEvents.Count, Is.EqualTo(1));
        });

        var evt = e.DomainEvents.Single() as ExerciseEventCreatedEvent;
        Assert.That(evt, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(evt!.EventId, Is.EqualTo(e.Id));
            Assert.That(evt!.UserId, Is.EqualTo((Guid)user));
            Assert.That(evt!.EventTime, Is.EqualTo(when));
            Assert.That(evt!.ExerciseTypeId, Is.EqualTo(type.Value));
            Assert.That(evt!.DurationMinutes, Is.EqualTo(duration.Minutes));
            Assert.That(evt!.Intensity, Is.EqualTo(IntensityType.Vigorous));
            Assert.That(evt!.Note, Is.EqualTo(note.Text));
            Assert.That(evt!.OccurredAt, Is.EqualTo(now));
            Assert.That(evt!.CorrelationId, Is.EqualTo(corr));
            Assert.That(evt!.CausationId, Is.EqualTo(caus));
        });
    }
}


