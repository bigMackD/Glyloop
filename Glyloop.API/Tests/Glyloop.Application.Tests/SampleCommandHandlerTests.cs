using AutoFixture;
using Glyloop.Application.Common.Interfaces;
using Glyloop.Domain.Common;
using Glyloop.Domain.Repositories;
using Glyloop.Domain.ValueObjects;
using NSubstitute;
using NUnit.Framework;

namespace Glyloop.Application.Tests;

/// <summary>
/// Sample test class demonstrating NUnit best practices for Application layer testing.
/// This example shows how to test command handlers with mocked dependencies.
/// </summary>
[TestFixture]
[Parallelizable(ParallelScope.All)]
[Category("Unit")]
public class SampleCommandHandlerTests
{
    private IFixture _fixture = null!;
    private IEventRepository _mockEventRepository = null!;
    private IUnitOfWork _mockUnitOfWork = null!;
    private ITimeProvider _mockTimeProvider = null!;

    [SetUp]
    public void SetUp()
    {
        // Arrange - Initialize AutoFixture and mocks
        _fixture = new Fixture();

        // Create mocks using NSubstitute
        _mockEventRepository = Substitute.For<IEventRepository>();
        _mockUnitOfWork = Substitute.For<IUnitOfWork>();
        _mockTimeProvider = Substitute.For<ITimeProvider>();

        // Set up common mock behavior
        _mockTimeProvider.UtcNow.Returns(new DateTimeOffset(2025, 11, 2, 12, 0, 0, TimeSpan.Zero));
    }

    /// <summary>
    /// Example of testing a command handler with successful path
    /// </summary>
    [Test]
    public async Task Handle_ShouldReturnSuccess_WhenCommandIsValid()
    {
        // Arrange
        var userId = UserId.Create(Guid.NewGuid());
        var carbohydrate = Carbohydrate.Create(45).Value;

        // Configure mock to return success
        _mockUnitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        // Act
        // This is a placeholder - replace with actual command handler logic
        await _mockUnitOfWork.SaveChangesAsync(CancellationToken.None);

        // Assert
        await _mockUnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Example of testing validation failure path
    /// </summary>
    [Test]
    public void Handle_ShouldThrowException_WhenValidationFails()
    {
        // Arrange
        var invalidUserId = Guid.Empty;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => UserId.Create(invalidUserId));
    }

    /// <summary>
    /// Example of testing with NSubstitute argument matching
    /// </summary>
    [Test]
    public async Task Handle_ShouldCallRepositoryWithCorrectArguments_WhenCommandIsValid()
    {
        // Arrange
        var userId = UserId.Create(Guid.NewGuid());
        var eventId = Guid.NewGuid();

        // Configure mock to capture arguments
        _mockEventRepository
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Glyloop.Domain.Aggregates.Event.Event?>(null));

        // Act
        await _mockEventRepository.GetByIdAsync(eventId, CancellationToken.None);

        // Assert - Verify method was called with specific argument
        await _mockEventRepository.Received(1)
            .GetByIdAsync(Arg.Is<Guid>(g => g == eventId), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Example of testing with multiple mock interactions
    /// </summary>
    [Test]
    public async Task Handle_ShouldFollowCorrectSequence_WhenProcessingCommand()
    {
        // Arrange
        var userId = UserId.Create(Guid.NewGuid());
        var eventId = Guid.NewGuid();

        // Configure mocks
        _mockEventRepository
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Glyloop.Domain.Aggregates.Event.Event?>(null));

        // Act - Simulate command handler flow
        await _mockEventRepository.GetByIdAsync(eventId, CancellationToken.None);
        await _mockUnitOfWork.SaveChangesAsync(CancellationToken.None);

        // Assert - Verify correct sequence of calls
        Received.InOrder(async () =>
        {
            await _mockEventRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
            await _mockUnitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>());
        });
    }

    /// <summary>
    /// Example of testing exception handling
    /// </summary>
    [Test]
    public void Handle_ShouldThrowException_WhenRepositoryFails()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        _mockEventRepository
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns<Glyloop.Domain.Aggregates.Event.Event?>(x => throw new InvalidOperationException("Database error"));

        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _mockEventRepository.GetByIdAsync(eventId, CancellationToken.None));
    }

    /// <summary>
    /// Example of testing with AutoFixture-generated complex objects
    /// </summary>
    [Test]
    public async Task Handle_ShouldProcessMultipleItems_WhenGeneratedByAutoFixture()
    {
        // Arrange
        var userIds = _fixture.CreateMany<Guid>(5).ToList();

        foreach (var id in userIds)
        {
            // Configure mock to return different results for each call
            _mockEventRepository
                .GetByIdAsync(id, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Glyloop.Domain.Aggregates.Event.Event?>(null));
        }

        // Act
        var tasks = userIds.Select(id =>
            _mockEventRepository.GetByIdAsync(id, CancellationToken.None));
        await Task.WhenAll(tasks);

        // Assert
        Assert.That(userIds, Has.Count.EqualTo(5));
        await _mockEventRepository.Received(5)
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Example of parameterized async test
    /// </summary>
    [TestCase("2025-01-01")]
    [TestCase("2025-06-15")]
    [TestCase("2025-12-31")]
    public async Task Handle_ShouldProcessDifferentDates_WhenTimeProviderReturnsVariousDates(string dateString)
    {
        // Arrange
        var date = DateTimeOffset.Parse(dateString);
        _mockTimeProvider.UtcNow.Returns(date);

        // Act
        var actualDate = _mockTimeProvider.UtcNow;
        await Task.CompletedTask; // Simulate async operation

        // Assert
        Assert.That(actualDate, Is.EqualTo(date));
    }

    /// <summary>
    /// Example of testing with cancellation token
    /// </summary>
    [Test]
    public void Handle_ShouldRespectCancellation_WhenTokenIsCancelled()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        _mockEventRepository
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns<object>(x =>
            {
                var token = x.ArgAt<CancellationToken>(1);
                token.ThrowIfCancellationRequested();
                return Task.FromResult<object?>(null);
            });

        // Act & Assert
        Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await _mockEventRepository.GetByIdAsync(Guid.NewGuid(), cancellationTokenSource.Token));
    }

    [TearDown]
    public void TearDown()
    {
        // Clean up mocks - NSubstitute doesn't require explicit cleanup
        // but you can clear any state if needed
    }
}

