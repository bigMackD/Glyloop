using System.Reflection;
using Glyloop.API.Mapping;
using Glyloop.Domain.Enums;
using NUnit.Framework;

namespace Glyloop.API.Tests;

/// <summary>
/// Unit tests for ContractMapper covering pure function mappings and parsing logic.
/// Tests critical business rules for data transformation between API contracts and domain models.
/// </summary>
[TestFixture]
[Category("Unit")]
public class ContractMapperTests
{
    #region InsulinType Parsing Tests

    [Test]
    public void ParseInsulinType_ShouldReturnFast_WhenValueIsFast()
    {
        // Arrange
        var input = "fast";

        // Act
        var result = ContractMapperTestsHelper.ParseInsulinType(input);

        // Assert
        Assert.That(result, Is.EqualTo(InsulinType.Fast));
    }

    [Test]
    public void ParseInsulinType_ShouldReturnFast_WhenValueIsFastWithDifferentCase()
    {
        // Arrange
        var inputs = new[] { "FAST", "Fast", "FaSt" };

        // Act & Assert
        foreach (var input in inputs)
        {
            var result = ContractMapperTestsHelper.ParseInsulinType(input);
            Assert.That(result, Is.EqualTo(InsulinType.Fast), $"Failed for input: {input}");
        }
    }

    [Test]
    public void ParseInsulinType_ShouldReturnLong_WhenValueIsLong()
    {
        // Arrange
        var input = "long";

        // Act
        var result = ContractMapperTestsHelper.ParseInsulinType(input);

        // Assert
        Assert.That(result, Is.EqualTo(InsulinType.Long));
    }

    [Test]
    public void ParseInsulinType_ShouldReturnLong_WhenValueIsLongWithDifferentCase()
    {
        // Arrange
        var inputs = new[] { "LONG", "Long", "LoNg" };

        // Act & Assert
        foreach (var input in inputs)
        {
            var result = ContractMapperTestsHelper.ParseInsulinType(input);
            Assert.That(result, Is.EqualTo(InsulinType.Long), $"Failed for input: {input}");
        }
    }

    [Test]
    public void ParseInsulinType_ShouldThrowNullReferenceException_WhenValueIsNull()
    {
        // Arrange & Act & Assert
        // Since we're using reflection, we need to handle the TargetInvocationException wrapper
        var ex = Assert.Throws<TargetInvocationException>(() =>
            ContractMapperTestsHelper.ParseInsulinType(null!));

        Assert.That(ex.InnerException, Is.InstanceOf<NullReferenceException>());
    }

    [TestCase("invalid")]
    [TestCase("rapid")]
    [TestCase("")]
    [TestCase("FAST_ACTING")]
    [TestCase("123")]
    public void ParseInsulinType_ShouldThrowArgumentException_WhenValueIsInvalid(string invalidValue)
    {
        // Arrange & Act & Assert
        // Since we're using reflection, we need to handle the TargetInvocationException wrapper
        var ex = Assert.Throws<TargetInvocationException>(() =>
            ContractMapperTestsHelper.ParseInsulinType(invalidValue));

        Assert.That(ex.InnerException, Is.InstanceOf<ArgumentException>());
        var argEx = ex.InnerException as ArgumentException;
        Assert.That(argEx!.Message, Contains.Substring($"Invalid insulin type: {invalidValue}"));
        Assert.That(argEx.ParamName, Is.EqualTo("value"));
    }

    #endregion

    #region AbsorptionHint Parsing Tests

    [Test]
    [TestCase("rapid", AbsorptionHint.Rapid)]
    [TestCase("RAPID", AbsorptionHint.Rapid)]
    [TestCase("Rapid", AbsorptionHint.Rapid)]
    [TestCase("normal", AbsorptionHint.Normal)]
    [TestCase("NORMAL", AbsorptionHint.Normal)]
    [TestCase("slow", AbsorptionHint.Slow)]
    [TestCase("SLOW", AbsorptionHint.Slow)]
    [TestCase("other", AbsorptionHint.Other)]
    [TestCase("OTHER", AbsorptionHint.Other)]
    public void ParseAbsorptionHint_ShouldReturnCorrectHint_WhenValueIsValid(string input, AbsorptionHint expected)
    {
        // Arrange & Act
        var result = ContractMapperTestsHelper.ParseAbsorptionHint(input);

        // Assert
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("invalid")]
    [TestCase("fast")]
    [TestCase("123")]
    public void ParseAbsorptionHint_ShouldReturnNull_WhenValueIsInvalid(string? invalidValue)
    {
        // Arrange & Act
        var result = ContractMapperTestsHelper.ParseAbsorptionHint(invalidValue);

        // Assert
        Assert.That(result, Is.Null);
    }

    #endregion

    #region IntensityType Parsing Tests

    [Test]
    [TestCase("light", IntensityType.Light)]
    [TestCase("LIGHT", IntensityType.Light)]
    [TestCase("Light", IntensityType.Light)]
    [TestCase("moderate", IntensityType.Moderate)]
    [TestCase("MODERATE", IntensityType.Moderate)]
    [TestCase("vigorous", IntensityType.Vigorous)]
    [TestCase("VIGOROUS", IntensityType.Vigorous)]
    public void ParseIntensityType_ShouldReturnCorrectIntensity_WhenValueIsValid(string input, IntensityType expected)
    {
        // Arrange & Act
        var result = ContractMapperTestsHelper.ParseIntensityType(input);

        // Assert
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("invalid")]
    [TestCase("high")]
    [TestCase("low")]
    [TestCase("123")]
    public void ParseIntensityType_ShouldReturnNull_WhenValueIsInvalid(string? invalidValue)
    {
        // Arrange & Act
        var result = ContractMapperTestsHelper.ParseIntensityType(invalidValue);

        // Assert
        Assert.That(result, Is.Null);
    }

    #endregion

    #region TimeRange Parsing Tests

    [Test]
    [TestCase("1", 1)]
    [TestCase("3", 3)]
    [TestCase("5", 5)]
    [TestCase("8", 8)]
    [TestCase("12", 12)]
    [TestCase("24", 24)]
    public void ParseTimeRange_ShouldReturnCorrectRange_WhenValueIsValidAllowedHour(string input, int expectedHours)
    {
        // Arrange & Act
        var (fromTime, toTime) = ContractMapperTestsHelper.ParseTimeRange(input);

        // Assert
        var expectedDuration = TimeSpan.FromHours(expectedHours);
        Assert.That(toTime - fromTime, Is.EqualTo(expectedDuration));
        Assert.That(fromTime, Is.LessThan(toTime));
    }

    [Test]
    public void ParseTimeRange_ShouldUseDefaultOf3Hours_WhenValueIsInvalidNumber()
    {
        // Arrange
        var invalidInput = "invalid";

        // Act
        var (fromTime, toTime) = ContractMapperTestsHelper.ParseTimeRange(invalidInput);

        // Assert
        var expectedDuration = TimeSpan.FromHours(3); // Default fallback
        Assert.That(toTime - fromTime, Is.EqualTo(expectedDuration));
    }

    [Test]
    public void ParseTimeRange_ShouldUseDefaultOf3Hours_WhenValueIsOutsideAllowedRange()
    {
        // Arrange
        var invalidInputs = new[] { "0", "2", "4", "6", "7", "9", "10", "11", "13", "25", "100" };

        // Act & Assert
        foreach (var input in invalidInputs)
        {
            var (fromTime, toTime) = ContractMapperTestsHelper.ParseTimeRange(input);
            var expectedDuration = TimeSpan.FromHours(3); // Default fallback
            Assert.That(toTime - fromTime, Is.EqualTo(expectedDuration), $"Failed for input: {input}");
        }
    }

    [Test]
    public void ParseTimeRange_ShouldReturnTimesRelativeToCurrentTime_WhenCalled()
    {
        // Arrange
        var input = "3";

        // Act
        var (fromTime, toTime) = ContractMapperTestsHelper.ParseTimeRange(input);

        // Assert
        Assert.That(toTime, Is.GreaterThan(fromTime)); // Ensure toTime is after fromTime
        var duration = toTime - fromTime;
        Assert.That(duration.TotalHours, Is.EqualTo(3.0)); // Ensure correct duration
        // The times should be reasonable - fromTime should be in the past (or very recent)
        // and toTime should be fromTime + 3 hours
        Assert.That(fromTime, Is.LessThan(DateTimeOffset.UtcNow.AddMinutes(1))); // Allow some tolerance
        Assert.That(toTime, Is.EqualTo(fromTime.AddHours(3))); // toTime should be exactly fromTime + 3 hours
    }

    #endregion

    #region Integration Tests for Complex Mappings

    [Test]
    public void ToCommand_ShouldMapFoodEventWithAbsorptionHint_WhenValidDataProvided()
    {
        // Arrange
        var request = new Glyloop.API.Contracts.Events.CreateFoodEventRequest
        {
            EventTime = DateTimeOffset.UtcNow,
            CarbohydratesGrams = 45,
            MealTagId = 1,
            AbsorptionHint = "rapid",
            Note = "Breakfast carbs"
        };

        // Act
        var command = request.ToCommand();

        // Assert
        Assert.That(command.EventTime, Is.EqualTo(request.EventTime));
        Assert.That(command.CarbohydratesGrams, Is.EqualTo(request.CarbohydratesGrams));
        Assert.That(command.MealTagId, Is.EqualTo(request.MealTagId));
        Assert.That(command.AbsorptionHint, Is.EqualTo(AbsorptionHint.Rapid));
        Assert.That(command.Note, Is.EqualTo(request.Note));
    }

    [Test]
    public void ToCommand_ShouldMapFoodEventWithNullAbsorptionHint_WhenAbsorptionHintIsInvalid()
    {
        // Arrange
        var request = new Glyloop.API.Contracts.Events.CreateFoodEventRequest
        {
            EventTime = DateTimeOffset.UtcNow,
            CarbohydratesGrams = 30,
            MealTagId = 2,
            AbsorptionHint = "invalid_hint",
            Note = "Snack"
        };

        // Act
        var command = request.ToCommand();

        // Assert
        Assert.That(command.AbsorptionHint, Is.Null);
    }

    [Test]
    public void ToCommand_ShouldMapInsulinEventWithInsulinType_WhenValidDataProvided()
    {
        // Arrange
        var request = new Glyloop.API.Contracts.Events.CreateInsulinEventRequest
        {
            EventTime = DateTimeOffset.UtcNow,
            InsulinType = "fast",
            InsulinUnits = 8.5m,
            Preparation = "Mixed with water",
            Delivery = "Subcutaneous injection",
            Timing = "Before meal",
            Note = "Morning dose"
        };

        // Act
        var command = request.ToCommand();

        // Assert
        Assert.That(command.EventTime, Is.EqualTo(request.EventTime));
        Assert.That(command.InsulinType, Is.EqualTo(InsulinType.Fast));
        Assert.That(command.Units, Is.EqualTo(request.InsulinUnits));
        Assert.That(command.Preparation, Is.EqualTo(request.Preparation));
        Assert.That(command.Delivery, Is.EqualTo(request.Delivery));
        Assert.That(command.Timing, Is.EqualTo(request.Timing));
        Assert.That(command.Note, Is.EqualTo(request.Note));
    }

    [Test]
    public void ToCommand_ShouldThrowException_WhenInsulinTypeIsInvalid()
    {
        // Arrange
        var request = new Glyloop.API.Contracts.Events.CreateInsulinEventRequest
        {
            EventTime = DateTimeOffset.UtcNow,
            InsulinType = "invalid_type",
            InsulinUnits = 5.0m
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => request.ToCommand());
    }

    [Test]
    public void ToCommand_ShouldMapExerciseEventWithIntensity_WhenValidDataProvided()
    {
        // Arrange
        var request = new Glyloop.API.Contracts.Events.CreateExerciseEventRequest
        {
            EventTime = DateTimeOffset.UtcNow,
            ExerciseTypeId = 1,
            DurationMinutes = 45,
            Intensity = "vigorous",
            Note = "Morning run"
        };

        // Act
        var command = request.ToCommand();

        // Assert
        Assert.That(command.EventTime, Is.EqualTo(request.EventTime));
        Assert.That(command.ExerciseTypeId, Is.EqualTo(request.ExerciseTypeId));
        Assert.That(command.DurationMinutes, Is.EqualTo(request.DurationMinutes));
        Assert.That(command.Intensity, Is.EqualTo(IntensityType.Vigorous));
        Assert.That(command.Note, Is.EqualTo(request.Note));
    }

    [Test]
    public void ToCommand_ShouldMapExerciseEventWithNullIntensity_WhenIntensityIsInvalid()
    {
        // Arrange
        var request = new Glyloop.API.Contracts.Events.CreateExerciseEventRequest
        {
            EventTime = DateTimeOffset.UtcNow,
            ExerciseTypeId = 2,
            DurationMinutes = 30,
            Intensity = "extreme",
            Note = "Weight lifting"
        };

        // Act
        var command = request.ToCommand();

        // Assert
        Assert.That(command.Intensity, Is.Null);
    }

    [Test]
    public void ToTirQuery_ShouldMapTimeRange_WhenValidRangeProvided()
    {
        // Arrange
        var range = "3";

        // Act
        var query = range.ToTirQuery();

        // Assert
        var timeSpan = query.ToTime - query.FromTime;
        Assert.That(timeSpan.TotalHours, Is.EqualTo(3));
    }

    #endregion
}

// Internal helper class to access private methods for testing
internal static class ContractMapperTestsHelper
{
    public static AbsorptionHint? ParseAbsorptionHint(string? value)
    {
        // Access private method via reflection for testing
        var method = typeof(Glyloop.API.Mapping.ContractMapper).GetMethod("ParseAbsorptionHint",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        return (AbsorptionHint?)method?.Invoke(null, new object?[] { value });
    }

    public static InsulinType ParseInsulinType(string value)
    {
        var method = typeof(Glyloop.API.Mapping.ContractMapper).GetMethod("ParseInsulinType",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        return (InsulinType)method?.Invoke(null, new[] { value })!;
    }

    public static IntensityType? ParseIntensityType(string? value)
    {
        var method = typeof(Glyloop.API.Mapping.ContractMapper).GetMethod("ParseIntensityType",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        return (IntensityType?)method?.Invoke(null, new object?[] { value });
    }

    public static (DateTimeOffset FromTime, DateTimeOffset ToTime) ParseTimeRange(string range)
    {
        var method = typeof(Glyloop.API.Mapping.ContractMapper).GetMethod("ParseTimeRange",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        return ((DateTimeOffset FromTime, DateTimeOffset ToTime))method?.Invoke(null, new[] { range })!;
    }
}
