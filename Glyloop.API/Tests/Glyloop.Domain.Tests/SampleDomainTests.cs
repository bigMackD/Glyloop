using AutoFixture;
using Glyloop.Domain.Common;
using Glyloop.Domain.ValueObjects;
using NSubstitute;
using NUnit.Framework;

namespace Glyloop.Domain.Tests;

/// <summary>
/// Sample test class demonstrating NUnit best practices for domain testing.
/// Follow AAA pattern: Arrange-Act-Assert
/// One behavior per test, descriptive naming: Method_ShouldDoX_WhenY
/// </summary>
[TestFixture]
[Parallelizable(ParallelScope.All)]
[Category("Unit")]
public class SampleDomainTests
{
    private IFixture _fixture = null!;

    [SetUp]
    public void SetUp()
    {
        // Initialize AutoFixture for test data generation
        _fixture = new Fixture();
    }

    [Test]
    public void Create_ShouldCreateUserId_WhenValidGuid()
    {
        // Arrange - Set up test data
        var validUserId = Guid.NewGuid();

        // Act - Execute the method under test
        var result = UserId.Create(validUserId);

        // Assert - Verify the expected outcome
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Value, Is.EqualTo(validUserId));
    }

    [Test]
    public void Create_ShouldThrowException_WhenEmptyUserId()
    {
        // Arrange
        var emptyUserId = Guid.Empty;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => UserId.Create(emptyUserId));
    }

    /// <summary>
    /// Example of parameterized testing using TestCase attribute
    /// </summary>
    [TestCase(-1)]
    [TestCase(-100)]
    [TestCase(301)]
    [TestCase(500)]
    public void Create_ShouldReturnFailure_WhenInvalidCarbohydrateAmount(int amount)
    {
        // Arrange & Act
        var result = Carbohydrate.Create(amount);

        // Assert
        Assert.That(result.IsFailure, Is.True);
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(50)]
    [TestCase(200)]
    [TestCase(300)]
    public void Create_ShouldReturnSuccess_WhenValidCarbohydrateAmount(int amount)
    {
        // Arrange & Act
        var result = Carbohydrate.Create(amount);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.Grams, Is.EqualTo(amount));
    }

    /// <summary>
    /// Example of testing multiple related assertions using Assert.Multiple
    /// </summary>
    [Test]
    public void Create_ShouldReturnValidCarbohydrate_WhenCreatedWithValidAmount()
    {
        // Arrange
        const int amount = 45;

        // Act
        var result = Carbohydrate.Create(amount);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.Grams, Is.EqualTo(amount));
            Assert.That(result.Value.ToString(), Does.Contain(amount.ToString()));
        });
    }

    /// <summary>
    /// Example using AutoFixture for generating test data
    /// </summary>
    [Test]
    public void Create_ShouldHandleMultipleValidUserIds_WhenGeneratedByAutoFixture()
    {
        // Arrange
        var userIds = _fixture.CreateMany<Guid>(10);

        // Act & Assert
        foreach (var userId in userIds)
        {
            var result = UserId.Create(userId);
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Value, Is.EqualTo(userId));
        }
    }

    /// <summary>
    /// Example of testing boundary conditions
    /// </summary>
    [Test]
    public void Create_ShouldHandleBoundaryValues_ForExerciseDuration()
    {
        // Arrange & Act
        var minDuration = ExerciseDuration.Create(1);
        var maxDuration = ExerciseDuration.Create(300); // 5 hours in minutes
        var belowMin = ExerciseDuration.Create(0);
        var exceedMax = ExerciseDuration.Create(301);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(minDuration.IsSuccess, Is.True, "Minimum duration should be valid");
            Assert.That(maxDuration.IsSuccess, Is.True, "Maximum duration should be valid");
            Assert.That(belowMin.IsFailure, Is.True, "Duration below min should fail");
            Assert.That(exceedMax.IsFailure, Is.True, "Duration exceeding max should fail");
        });
    }

    /// <summary>
    /// Example of async test
    /// </summary>
    [Test]
    public async Task SampleAsyncTest_ShouldComplete_WhenCalled()
    {
        // Arrange
        var taskDelay = Task.FromResult(true);

        // Act
        var result = await taskDelay;

        // Assert
        Assert.That(result, Is.True);
    }

    /// <summary>
    /// Example of testing with NSubstitute mock (for interfaces)
    /// Note: In domain tests, we typically don't mock value objects,
    /// but this demonstrates the syntax for when you need it in Application/Infrastructure tests
    /// </summary>
    [Test]
    public void Sample_ShouldDemonstrateNSubstitute_WhenMockingInterfaces()
    {
        // Arrange
        var mockTimeProvider = Substitute.For<ITimeProvider>();
        var expectedTime = new DateTimeOffset(2025, 11, 2, 12, 0, 0, TimeSpan.Zero);
        mockTimeProvider.UtcNow.Returns(expectedTime);

        // Act
        var actualTime = mockTimeProvider.UtcNow;

        // Assert
        Assert.That(actualTime, Is.EqualTo(expectedTime));
        // Verify the property was accessed
        _ = mockTimeProvider.Received(1).UtcNow;
    }

    [TearDown]
    public void TearDown()
    {
        // Clean up resources if needed
        // For domain tests, usually nothing to clean up
    }
}

