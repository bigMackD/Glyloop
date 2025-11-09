using Glyloop.Application.Common.Interfaces;
using Glyloop.Application.DTOs.Events;
using Glyloop.Application.Queries.Events.GetEventOutcome;
using Glyloop.Domain.Aggregates.Event;
using Glyloop.Domain.Common;
using Glyloop.Domain.Repositories;
using Glyloop.Domain.ValueObjects;
using NSubstitute;
using NUnit.Framework;

namespace Glyloop.Application.Tests;

/// <summary>
/// Unit tests for GetEventOutcomeQueryHandler covering not found, authorization, invalid type,
/// upstream glucose service failures, and both null and present reading scenarios.
/// </summary>
[TestFixture]
[Category("Unit")]
public class GetEventOutcomeQueryHandlerTests
{
    private IEventRepository _eventRepository = null!;
    private IGlucoseReadingService _glucoseService = null!;
    private ICurrentUserService _currentUserService = null!;
    private GetEventOutcomeQueryHandler _sut = null!;

    private readonly Guid _userId = Guid.Parse("aaaaaaaa-0000-0000-0000-bbbbbbbbbbbb");

    [SetUp]
    public void SetUp()
    {
        _eventRepository = Substitute.For<IEventRepository>();
        _glucoseService = Substitute.For<IGlucoseReadingService>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _currentUserService.UserId.Returns(_userId);

        _sut = new GetEventOutcomeQueryHandler(_eventRepository, _glucoseService, _currentUserService);
    }

    [Test]
    public async Task Handle_NotFound_ShouldReturnFailure()
    {
        // Arrange
        var id = Guid.NewGuid();
        _eventRepository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Event?)null);

        // Act
        var result = await _sut.Handle(new GetEventOutcomeQuery(id), CancellationToken.None);

        // Assert
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error.Code, Is.EqualTo("Event.NotFound"));
    }

    [Test]
    public async Task Handle_NotOwned_ShouldReturnForbidden()
    {
        // Arrange
        var id = Guid.NewGuid();
        var other = UserId.Create(Guid.NewGuid());
        var tp = new FixedTimeProvider(DateTimeOffset.UtcNow);
        var food = FoodEvent.Create(other, DateTimeOffset.UtcNow.AddMinutes(-10), Carbohydrate.Create(10).Value,
            MealTagId.Create(1), Domain.Enums.AbsorptionHint.Normal, null, Domain.Enums.SourceType.Manual, tp, Guid.NewGuid(), Guid.NewGuid()).Value;
        _eventRepository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(food);

        // Act
        var result = await _sut.Handle(new GetEventOutcomeQuery(id), CancellationToken.None);

        // Assert
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error.Code, Is.EqualTo("Authorization.Forbidden"));
    }

    [Test]
    public async Task Handle_NotFoodEvent_ShouldReturnInvalidType()
    {
        // Arrange
        var id = Guid.NewGuid();
        var user = UserId.Create(_userId);
        var tp = new FixedTimeProvider(DateTimeOffset.UtcNow);
        var note = NoteEvent.Create(user, DateTimeOffset.UtcNow.AddMinutes(-5), NoteText.Create("x").Value,
            Domain.Enums.SourceType.Manual, tp, Guid.NewGuid(), Guid.NewGuid()).Value;
        _eventRepository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(note);

        // Act
        var result = await _sut.Handle(new GetEventOutcomeQuery(id), CancellationToken.None);

        // Assert
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error.Code, Is.EqualTo("Event.InvalidType"));
    }

    [Test]
    public async Task Handle_GlucoseServiceFailure_ShouldPropagateFailure()
    {
        // Arrange
        var id = Guid.NewGuid();
        var user = UserId.Create(_userId);
        var when = DateTimeOffset.UtcNow.AddHours(-3);
        var tp = new FixedTimeProvider(when.AddMinutes(1));
        var food = FoodEvent.Create(user, when, Carbohydrate.Create(20).Value, MealTagId.Create(1),
            Domain.Enums.AbsorptionHint.Normal, null, Domain.Enums.SourceType.Manual, tp, Guid.NewGuid(), Guid.NewGuid()).Value;
        _eventRepository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(food);

        // reading near event+2h
        var target = when.AddHours(2);
        _glucoseService.GetReadingNearTimeAsync(user, target, 15, Arg.Any<CancellationToken>())
            .Returns(Result.Failure<GlucoseReading?>(Error.Create("Dexcom.Error", "unavailable")));

        // Act
        var result = await _sut.Handle(new GetEventOutcomeQuery(id), CancellationToken.None);

        // Assert
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error.Code, Is.EqualTo("Dexcom.Error"));
    }

    [Test]
    public async Task Handle_NoReadingFound_ShouldReturnSuccessWithApproximateOutcome()
    {
        // Arrange
        var id = Guid.NewGuid();
        var user = UserId.Create(_userId);
        var when = DateTimeOffset.UtcNow.AddHours(-2);
        var tp = new FixedTimeProvider(when.AddMinutes(1));
        var food = FoodEvent.Create(user, when, Carbohydrate.Create(45).Value, MealTagId.Create(3),
            Domain.Enums.AbsorptionHint.Normal, null, Domain.Enums.SourceType.Manual, tp, Guid.NewGuid(), Guid.NewGuid()).Value;
        _eventRepository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(food);

        var target = when.AddHours(2);
        _glucoseService.GetReadingNearTimeAsync(user, target, 15, Arg.Any<CancellationToken>())
            .Returns(Result.Success<GlucoseReading?>(null));

        // Act
        var result = await _sut.Handle(new GetEventOutcomeQuery(id), CancellationToken.None);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        var dto = result.Value;
        Assert.That(dto.EventId, Is.EqualTo(food.Id));
        Assert.That(dto.TargetTime, Is.EqualTo(target));
        Assert.That(dto.HasReading, Is.False);
        Assert.That(dto.GlucoseValueMgDl, Is.Null);
        Assert.That(dto.ReadingTime, Is.Null);
    }

    [Test]
    public async Task Handle_ReadingFound_ShouldReturnSuccessWithReadingDetails()
    {
        // Arrange
        var id = Guid.NewGuid();
        var user = UserId.Create(_userId);
        var when = DateTimeOffset.UtcNow.AddHours(-4);
        var tp = new FixedTimeProvider(when.AddMinutes(1));
        var food = FoodEvent.Create(user, when, Carbohydrate.Create(30).Value, MealTagId.Create(2),
            Domain.Enums.AbsorptionHint.Normal, null, Domain.Enums.SourceType.Manual, tp, Guid.NewGuid(), Guid.NewGuid()).Value;
        _eventRepository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(food);

        var target = when.AddHours(2);
        var reading = new GlucoseReading(target.AddMinutes(2), 155, "Rising");
        _glucoseService.GetReadingNearTimeAsync(user, target, 15, Arg.Any<CancellationToken>())
            .Returns(Result.Success<GlucoseReading?>(reading));

        // Act
        var result = await _sut.Handle(new GetEventOutcomeQuery(id), CancellationToken.None);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        var dto = result.Value;
        Assert.That(dto.HasReading, Is.True);
        Assert.That(dto.GlucoseValueMgDl, Is.EqualTo(155));
        Assert.That(dto.ReadingTime, Is.EqualTo(reading.SystemTime));
    }

    private sealed class FixedTimeProvider : ITimeProvider
    {
        public FixedTimeProvider(DateTimeOffset utcNow) => UtcNow = utcNow;
        public DateTimeOffset UtcNow { get; }
    }
}


