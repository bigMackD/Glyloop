using Glyloop.Application.Common.Interfaces;
using Glyloop.Application.DTOs.Common;
using Glyloop.Application.DTOs.Events;
using Glyloop.Application.Queries.Events.ListEvents;
using Glyloop.Domain.Aggregates.Event;
using Glyloop.Domain.Common;
using Glyloop.Domain.Enums;
using Glyloop.Domain.Repositories;
using Glyloop.Domain.ValueObjects;
using NSubstitute;
using NUnit.Framework;

namespace Glyloop.Application.Tests;

/// <summary>
/// Unit tests for ListEventsQueryHandler covering pagination wiring and summary generation.
/// Includes a test for default date window using approximate assertions to avoid flakiness.
/// </summary>
[TestFixture]
[Category("Unit")]
public class ListEventsQueryHandlerTests
{
    private IEventRepository _eventRepository = null!;
    private ICurrentUserService _currentUserService = null!;
    private ListEventsQueryHandler _sut = null!;

    private readonly Guid _userId = Guid.Parse("11111111-2222-3333-4444-555555555555");

    [SetUp]
    public void SetUp()
    {
        _eventRepository = Substitute.For<IEventRepository>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _currentUserService.UserId.Returns(_userId);

        _sut = new ListEventsQueryHandler(_eventRepository, _currentUserService);
    }

    [Test]
    public async Task Handle_WithExplicitDates_ShouldPageAndMapSummaries()
    {
        // Arrange
        var userId = UserId.Create(_userId);
        var from = new DateTimeOffset(2024, 09, 01, 0, 0, 0, TimeSpan.Zero);
        var to = new DateTimeOffset(2024, 10, 01, 0, 0, 0, TimeSpan.Zero);

        var events = new List<Event>
        {
            CreateFoodEvent(userId, from.AddDays(1), 60, 1, "Dinner"),
            CreateInsulinEvent(userId, from.AddDays(2), InsulinType.Long, 14.0m, null),
            CreateExerciseEvent(userId, from.AddDays(3), 45, IntensityType.Vigorous, "Run"),
            CreateNoteEvent(userId, from.AddDays(4), new string('a', 60)) // truncates to 47 + "..."
        };

        _eventRepository.CountByUserIdAsync(userId, from, to, null, Arg.Any<CancellationToken>())
            .Returns(events.Count);
        _eventRepository.GetPagedAsync(userId, from, to, 2, 2, Arg.Any<CancellationToken>())
            .Returns(events.Take(2).ToList());

        var query = new ListEventsQuery(null, from, to, Page: 2, PageSize: 2);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        var page = result.Value;

        Assert.That(page.TotalCount, Is.EqualTo(events.Count));
        Assert.That(page.Items.Count, Is.EqualTo(2));

        // Summary assertions (per handler switch expression)
        Assert.That(page.Items[0].Summary, Is.EqualTo("60g carbs"));
        Assert.That(page.Items[1].Summary, Is.EqualTo("14.0U Long"));
    }

    [Test]
    public async Task Handle_DefaultDateWindow_ShouldUseLast30Days_Approximately()
    {
        // Arrange
        var userId = UserId.Create(_userId);

        // Capture arguments passed to repository to inspect computed dates
        DateTimeOffset capturedFrom = default;
        DateTimeOffset capturedTo = default;

        _eventRepository
            .CountByUserIdAsync(userId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), null, Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                capturedFrom = callInfo.ArgAt<DateTimeOffset>(1);
                capturedTo = callInfo.ArgAt<DateTimeOffset>(2);
                return 0;
            });

        _eventRepository
            .GetPagedAsync(userId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), 1, 20, Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                // also capture these
                capturedFrom = callInfo.ArgAt<DateTimeOffset>(1);
                capturedTo = callInfo.ArgAt<DateTimeOffset>(2);
                return Array.Empty<Event>();
            });

        var query = new ListEventsQuery(null, null, null);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        // Approximate assertions: Ensure window is ~30 days and 'to' is close to now
        var span = capturedTo - capturedFrom;
        Assert.That(span.TotalDays, Is.InRange(29, 31));
        Assert.That((DateTimeOffset.UtcNow - capturedTo).TotalSeconds, Is.LessThan(5)); // within a few seconds
    }

    private static FoodEvent CreateFoodEvent(UserId userId, DateTimeOffset when, int carbs, int mealTagId, string? note)
    {
        var tp = new FixedTimeProvider(when.AddHours(1));
        return FoodEvent.Create(userId, when, Carbohydrate.Create(carbs).Value, MealTagId.Create(mealTagId),
            AbsorptionHint.Normal, NoteText.CreateOptional(note), SourceType.Manual, tp, Guid.NewGuid(), Guid.NewGuid()).Value;
    }

    private static InsulinEvent CreateInsulinEvent(UserId userId, DateTimeOffset when, InsulinType type, decimal units, string? note)
    {
        var tp = new FixedTimeProvider(when.AddHours(1));
        return InsulinEvent.Create(userId, when, type, InsulinDose.Create(units).Value, null, null, null,
            NoteText.CreateOptional(note), SourceType.Manual, tp, Guid.NewGuid(), Guid.NewGuid()).Value;
    }

    private static ExerciseEvent CreateExerciseEvent(UserId userId, DateTimeOffset when, int minutes, IntensityType intensity, string? note)
    {
        var tp = new FixedTimeProvider(when.AddHours(1));
        return ExerciseEvent.Create(userId, when, ExerciseTypeId.Create(1), ExerciseDuration.Create(minutes).Value,
            intensity, NoteText.CreateOptional(note), SourceType.Manual, tp, Guid.NewGuid(), Guid.NewGuid()).Value;
    }

    private static NoteEvent CreateNoteEvent(UserId userId, DateTimeOffset when, string text)
    {
        var tp = new FixedTimeProvider(when.AddHours(1));
        return NoteEvent.Create(userId, when, NoteText.Create(text).Value, SourceType.Manual, tp, Guid.NewGuid(), Guid.NewGuid()).Value;
    }

    private sealed class FixedTimeProvider : ITimeProvider
    {
        public FixedTimeProvider(DateTimeOffset utcNow) => UtcNow = utcNow;
        public DateTimeOffset UtcNow { get; }
    }
}


