using Glyloop.Application.Common.Interfaces;
using Glyloop.Application.DTOs.Chart;
using Glyloop.Application.Queries.Chart.GetChartData;
using Glyloop.Domain.Aggregates.Event;
using Glyloop.Domain.Common;
using Glyloop.Domain.Enums;
using Glyloop.Domain.Repositories;
using Glyloop.Domain.ValueObjects;
using NSubstitute;
using NUnit.Framework;

namespace Glyloop.Application.Tests;

/// <summary>
/// Unit tests for GetChartDataQueryHandler covering range validation,
/// time window computation, upstream failures, and mapping to DTOs with tooltips.
/// </summary>
[TestFixture]
[Category("Unit")]
public class GetChartDataQueryHandlerTests
{
    private IGlucoseReadingService _glucoseReadingService = null!;
    private IEventRepository _eventRepository = null!;
    private ICurrentUserService _currentUserService = null!;
    private ITimeProvider _timeProvider = null!;
    private GetChartDataQueryHandler _sut = null!;

    private readonly Guid _userId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeffffffff");
    private readonly DateTimeOffset _now = new(2024, 10, 01, 12, 00, 00, TimeSpan.Zero);

    [SetUp]
    public void SetUp()
    {
        _glucoseReadingService = Substitute.For<IGlucoseReadingService>();
        _eventRepository = Substitute.For<IEventRepository>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _timeProvider = Substitute.For<ITimeProvider>();

        _currentUserService.UserId.Returns(_userId);
        _timeProvider.UtcNow.Returns(_now);

        _sut = new GetChartDataQueryHandler(
            _glucoseReadingService,
            _eventRepository,
            _currentUserService,
            _timeProvider);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("abc")]
    [TestCase("2")] // not in allowed set
    [TestCase("25")] // not in allowed set
    public async Task Handle_InvalidRange_ShouldReturnFailure(string? range)
    {
        // Act
        var result = await _sut.Handle(new GetChartDataQuery(range ?? string.Empty), CancellationToken.None);

        // Assert
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error.Code, Is.EqualTo("Chart.InvalidRange"));
    }

    [Test]
    public async Task Handle_GlucoseServiceFailure_ShouldPropagateFailure()
    {
        // Arrange
        var end = _now;
        var start = end.AddHours(-3);
        _glucoseReadingService
            .GetReadingsInRangeAsync(UserId.Create(_userId), start, end, Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyList<GlucoseReading>>(Error.Create("Dexcom.Error", "upstream failure")));

        // Act
        var result = await _sut.Handle(new GetChartDataQuery("3"), CancellationToken.None);

        // Assert
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error.Code, Is.EqualTo("Dexcom.Error"));
    }

    [Test]
    public async Task Handle_ValidRange_ShouldReturnMappedDto_WithGlucoseAndEventOverlays()
    {
        // Arrange
        var end = _now;
        var start = end.AddHours(-1);
        var userId = UserId.Create(_userId);

        var readings = new List<GlucoseReading>
        {
            new(end.AddMinutes(-50), 110, "Flat"),
            new(end.AddMinutes(-20), 140, "Rising"),
        };

        _glucoseReadingService
            .GetReadingsInRangeAsync(userId, start, end, Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyList<GlucoseReading>>(readings));

        var events = new List<Event>
        {
            CreateFoodEvent(userId, end.AddMinutes(-55), 45, mealTagId: 2, AbsorptionHint.Normal, note: "Lunch"),
            CreateInsulinEvent(userId, end.AddMinutes(-25), InsulinType.Fast, 4.5m, note: "Bolus"),
            CreateExerciseEvent(userId, end.AddMinutes(-75), 30, IntensityType.Moderate, note: "Walk"),
            CreateNoteEvent(userId, end.AddMinutes(-10), new string('x', 40)), // will be truncated in tooltip
        };

        _eventRepository
            .GetByUserIdAsync(userId, null, start, end, Arg.Any<CancellationToken>())
            .Returns(events);

        // Act
        var result = await _sut.Handle(new GetChartDataQuery("1"), CancellationToken.None);

        // Assert
        Assert.That(result.IsSuccess, Is.True);

        var dto = result.Value;
        Assert.That(dto.StartTime, Is.EqualTo(start));
        Assert.That(dto.EndTime, Is.EqualTo(end));

        Assert.That(dto.GlucoseData.Count, Is.EqualTo(2));
        Assert.That(dto.GlucoseData[0], Is.EqualTo(new GlucoseDataPointDto(readings[0].SystemTime, readings[0].ValueMgDl, readings[0].Trend)));

        Assert.That(dto.Events.Count, Is.EqualTo(4));
        // tooltips per type:
        Assert.That(dto.Events[0].Tooltip, Is.EqualTo("45g carbs"));
        Assert.That(dto.Events[1].Tooltip, Is.EqualTo("4.5U Fast"));
        Assert.That(dto.Events[2].Tooltip, Is.EqualTo("30min exercise"));
        Assert.That(dto.Events[3].Tooltip.EndsWith("..."), Is.True); // truncated note
    }

    [Test]
    public async Task Handle_EmptyData_ShouldReturnSuccessWithEmptyCollections()
    {
        // Arrange
        var end = _now;
        var start = end.AddHours(-5);
        var userId = UserId.Create(_userId);

        _glucoseReadingService
            .GetReadingsInRangeAsync(userId, start, end, Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyList<GlucoseReading>>(Array.Empty<GlucoseReading>()));

        _eventRepository
            .GetByUserIdAsync(userId, null, start, end, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Event>());

        // Act
        var result = await _sut.Handle(new GetChartDataQuery("5"), CancellationToken.None);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.GlucoseData, Is.Empty);
        Assert.That(result.Value.Events, Is.Empty);
    }

    private static FoodEvent CreateFoodEvent(
        UserId userId,
        DateTimeOffset when,
        int carbs,
        int mealTagId,
        AbsorptionHint absorptionHint,
        string? note)
    {
        var time = new FixedTimeProvider(when.AddHours(1)); // ensure not in future
        var carb = Carbohydrate.Create(carbs).Value;
        var mealTag = MealTagId.Create(mealTagId);
        var noteText = NoteText.CreateOptional(note);
        return FoodEvent.Create(userId, when, carb, mealTag, absorptionHint, noteText, SourceType.Manual, time, Guid.NewGuid(), Guid.NewGuid()).Value;
    }

    private static InsulinEvent CreateInsulinEvent(
        UserId userId,
        DateTimeOffset when,
        InsulinType type,
        decimal units,
        string? note)
    {
        var time = new FixedTimeProvider(when.AddHours(1));
        var dose = InsulinDose.Create(units).Value;
        var noteText = NoteText.CreateOptional(note);
        return InsulinEvent.Create(userId, when, type, dose, null, null, null, noteText, SourceType.Manual, time, Guid.NewGuid(), Guid.NewGuid()).Value;
    }

    private static ExerciseEvent CreateExerciseEvent(
        UserId userId,
        DateTimeOffset when,
        int minutes,
        IntensityType intensity,
        string? note)
    {
        var time = new FixedTimeProvider(when.AddHours(1));
        var duration = ExerciseDuration.Create(minutes).Value;
        var noteText = NoteText.CreateOptional(note);
        var typeId = ExerciseTypeId.Create(1);
        return ExerciseEvent.Create(userId, when, typeId, duration, intensity, noteText, SourceType.Manual, time, Guid.NewGuid(), Guid.NewGuid()).Value;
    }

    private static NoteEvent CreateNoteEvent(
        UserId userId,
        DateTimeOffset when,
        string text)
    {
        var time = new FixedTimeProvider(when.AddHours(1));
        var note = NoteText.Create(text).Value;
        return NoteEvent.Create(userId, when, note, SourceType.Manual, time, Guid.NewGuid(), Guid.NewGuid()).Value;
    }

    private sealed class FixedTimeProvider : ITimeProvider
    {
        public FixedTimeProvider(DateTimeOffset utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTimeOffset UtcNow { get; }
    }
}


