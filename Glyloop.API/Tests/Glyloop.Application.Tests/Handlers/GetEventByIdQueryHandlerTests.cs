using Glyloop.Application.Common.Interfaces;
using Glyloop.Application.DTOs.Events;
using Glyloop.Application.Queries.Events.GetEventById;
using Glyloop.Domain.Aggregates.Event;
using Glyloop.Domain.Common;
using Glyloop.Domain.Enums;
using Glyloop.Domain.Repositories;
using Glyloop.Domain.ValueObjects;
using NSubstitute;
using NUnit.Framework;

namespace Glyloop.Application.Tests;

/// <summary>
/// Unit tests for GetEventByIdQueryHandler covering not found, ownership, and per-type DTO mapping.
/// </summary>
[TestFixture]
[Category("Unit")]
public class GetEventByIdQueryHandlerTests
{
    private IEventRepository _eventRepository = null!;
    private ICurrentUserService _currentUserService = null!;
    private GetEventByIdQueryHandler _sut = null!;

    private readonly Guid _userId = Guid.Parse("22222222-3333-4444-5555-666666666666");

    [SetUp]
    public void SetUp()
    {
        _eventRepository = Substitute.For<IEventRepository>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _currentUserService.UserId.Returns(_userId);
        _sut = new GetEventByIdQueryHandler(_eventRepository, _currentUserService);
    }

    [Test]
    public async Task Handle_NotFound_ShouldReturnFailure()
    {
        // Arrange
        var id = Guid.NewGuid();
        _eventRepository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Event?)null);

        // Act
        var result = await _sut.Handle(new GetEventByIdQuery(id), CancellationToken.None);

        // Assert
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error.Code, Is.EqualTo("Event.NotFound"));
    }

    [Test]
    public async Task Handle_NotOwnedByUser_ShouldReturnForbidden()
    {
        // Arrange
        var id = Guid.NewGuid();
        var otherUser = UserId.Create(Guid.NewGuid());
        var tp = new FixedTimeProvider(DateTimeOffset.UtcNow);
        var food = FoodEvent.Create(otherUser, DateTimeOffset.UtcNow.AddMinutes(-10), Carbohydrate.Create(10).Value,
            MealTagId.Create(1), AbsorptionHint.Normal, null, SourceType.Manual, tp, Guid.NewGuid(), Guid.NewGuid()).Value;
        _eventRepository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(food);

        // Act
        var result = await _sut.Handle(new GetEventByIdQuery(id), CancellationToken.None);

        // Assert
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error.Code, Is.EqualTo("Authorization.Forbidden"));
    }

    [Test]
    public async Task Handle_FoodEvent_ShouldMapToFoodEventDto()
    {
        // Arrange
        var id = Guid.NewGuid();
        var user = UserId.Create(_userId);
        var when = DateTimeOffset.UtcNow.AddMinutes(-30);
        var tp = new FixedTimeProvider(when.AddMinutes(1));
        var note = NoteText.CreateOptional("Pasta");
        var food = FoodEvent.Create(user, when, Carbohydrate.Create(80).Value, MealTagId.Create(2), AbsorptionHint.Slow, note,
            SourceType.Manual, tp, Guid.NewGuid(), Guid.NewGuid()).Value;

        _eventRepository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(food);

        // Act
        var result = await _sut.Handle(new GetEventByIdQuery(id), CancellationToken.None);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Is.TypeOf<FoodEventDto>());
        var dto = (FoodEventDto)result.Value;
        Assert.That(dto.EventId, Is.EqualTo(food.Id));
        Assert.That(dto.UserId, Is.EqualTo(_userId));
        Assert.That(dto.EventType, Is.EqualTo(EventType.Food));
        Assert.That(dto.CarbohydratesGrams, Is.EqualTo(food.Carbohydrates.Grams));
        Assert.That(dto.MealTagId, Is.EqualTo(food.MealTag.Value));
        Assert.That(dto.AbsorptionHint, Is.EqualTo(food.AbsorptionHint));
        Assert.That(dto.Note, Is.EqualTo(note?.Text));
    }

    [Test]
    public async Task Handle_InsulinEvent_ShouldMapToInsulinEventDto()
    {
        // Arrange
        var id = Guid.NewGuid();
        var user = UserId.Create(_userId);
        var when = DateTimeOffset.UtcNow.AddMinutes(-20);
        var tp = new FixedTimeProvider(when.AddMinutes(1));
        var note = NoteText.CreateOptional("Pre-meal");
        var insulin = InsulinEvent.Create(user, when, InsulinType.Fast, InsulinDose.Create(6.5m).Value, "Humalog", "Injection", "Before meal",
            note, SourceType.Manual, tp, Guid.NewGuid(), Guid.NewGuid()).Value;

        _eventRepository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(insulin);

        // Act
        var result = await _sut.Handle(new GetEventByIdQuery(id), CancellationToken.None);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Is.TypeOf<InsulinEventDto>());
        var dto = (InsulinEventDto)result.Value;
        Assert.That(dto.UserId, Is.EqualTo(_userId));
        Assert.That(dto.InsulinType, Is.EqualTo(InsulinType.Fast));
        Assert.That(dto.Units, Is.EqualTo(6.5m));
        Assert.That(dto.Preparation, Is.EqualTo("Humalog"));
        Assert.That(dto.Delivery, Is.EqualTo("Injection"));
        Assert.That(dto.Timing, Is.EqualTo("Before meal"));
    }

    [Test]
    public async Task Handle_ExerciseEvent_ShouldMapToExerciseEventDto()
    {
        // Arrange
        var id = Guid.NewGuid();
        var user = UserId.Create(_userId);
        var when = DateTimeOffset.UtcNow.AddMinutes(-90);
        var tp = new FixedTimeProvider(when.AddMinutes(1));
        var note = NoteText.CreateOptional("Evening ride");
        var exercise = ExerciseEvent.Create(user, when, ExerciseTypeId.Create(3), ExerciseDuration.Create(50).Value, IntensityType.Moderate,
            note, SourceType.Manual, tp, Guid.NewGuid(), Guid.NewGuid()).Value;

        _eventRepository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(exercise);

        // Act
        var result = await _sut.Handle(new GetEventByIdQuery(id), CancellationToken.None);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Is.TypeOf<ExerciseEventDto>());
        var dto = (ExerciseEventDto)result.Value;
        Assert.That(dto.UserId, Is.EqualTo(_userId));
        Assert.That(dto.ExerciseTypeId, Is.EqualTo(3));
        Assert.That(dto.DurationMinutes, Is.EqualTo(50));
        Assert.That(dto.Intensity, Is.EqualTo(IntensityType.Moderate));
    }

    [Test]
    public async Task Handle_NoteEvent_ShouldMapToNoteEventDto()
    {
        // Arrange
        var id = Guid.NewGuid();
        var user = UserId.Create(_userId);
        var when = DateTimeOffset.UtcNow.AddMinutes(-5);
        var tp = new FixedTimeProvider(when.AddMinutes(1));
        var text = NoteText.Create("Feeling low").Value;
        var noteEvent = NoteEvent.Create(user, when, text, SourceType.Manual, tp, Guid.NewGuid(), Guid.NewGuid()).Value;

        _eventRepository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(noteEvent);

        // Act
        var result = await _sut.Handle(new GetEventByIdQuery(id), CancellationToken.None);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Is.TypeOf<NoteEventDto>());
        var dto = (NoteEventDto)result.Value;
        Assert.That(dto.UserId, Is.EqualTo(_userId));
        Assert.That(dto.Note, Is.Null); // per mapping
        Assert.That(dto.Text, Is.EqualTo("Feeling low"));
    }

    private sealed class FixedTimeProvider : ITimeProvider
    {
        public FixedTimeProvider(DateTimeOffset utcNow) => UtcNow = utcNow;
        public DateTimeOffset UtcNow { get; }
    }
}


