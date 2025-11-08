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
[Category("Unit")]
public class SampleCommandHandlerTests
{
    // Note: Removed [Parallelizable] to avoid mock state interference between tests
    // Each test creates fresh mocks to ensure isolation and determinism

    private IFixture CreateFixture()
    {
        return new Fixture();
    }

    /// <summary>
    /// Example of testing a command handler with successful path
    /// </summary>
    [Test]
    public async Task Handle_ShouldReturnSuccess_WhenCommandIsValid()
    {
        // Arrange
        var mockUnitOfWork = Substitute.For<IUnitOfWork>();
        var userId = UserId.Create(Guid.NewGuid());
        var carbohydrate = Carbohydrate.Create(45).Value;

        // Configure mock to return success
        mockUnitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        // Act
        // This is a placeholder - replace with actual command handler logic
        await mockUnitOfWork.SaveChangesAsync(CancellationToken.None);

        // Assert
        await mockUnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
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
        var mockEventRepository = Substitute.For<IEventRepository>();
        var userId = UserId.Create(Guid.NewGuid());
        var eventId = Guid.NewGuid();

        // Configure mock to capture arguments
        mockEventRepository
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Glyloop.Domain.Aggregates.Event.Event?>(null));

        // Act
        await mockEventRepository.GetByIdAsync(eventId, CancellationToken.None);

        // Assert - Verify method was called with specific argument
        await mockEventRepository.Received(1)
            .GetByIdAsync(Arg.Is<Guid>(g => g == eventId), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Example of testing with multiple mock interactions
    /// </summary>
    [Test]
    public async Task Handle_ShouldFollowCorrectSequence_WhenProcessingCommand()
    {
        // Arrange
        var mockEventRepository = Substitute.For<IEventRepository>();
        var mockUnitOfWork = Substitute.For<IUnitOfWork>();
        var userId = UserId.Create(Guid.NewGuid());
        var eventId = Guid.NewGuid();

        // Configure mocks
        mockEventRepository
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Glyloop.Domain.Aggregates.Event.Event?>(null));

        // Act - Simulate command handler flow
        await mockEventRepository.GetByIdAsync(eventId, CancellationToken.None);
        await mockUnitOfWork.SaveChangesAsync(CancellationToken.None);

        // Assert - Verify correct sequence of calls
        Received.InOrder(async () =>
        {
            await mockEventRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
            await mockUnitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>());
        });
    }

    /// <summary>
    /// Example of testing exception handling
    /// </summary>
    [Test]
    public void Handle_ShouldThrowException_WhenRepositoryFails()
    {
        // Arrange
        var mockEventRepository = Substitute.For<IEventRepository>();
        var eventId = Guid.NewGuid();
        mockEventRepository
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns<Glyloop.Domain.Aggregates.Event.Event?>(x => throw new InvalidOperationException("Database error"));

        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await mockEventRepository.GetByIdAsync(eventId, CancellationToken.None));
    }

    /// <summary>
    /// Example of testing with AutoFixture-generated complex objects
    /// </summary>
    [Test]
    public async Task Handle_ShouldProcessMultipleItems_WhenGeneratedByAutoFixture()
    {
        // Arrange
        var fixture = CreateFixture();
        var mockEventRepository = Substitute.For<IEventRepository>();
        var userIds = fixture.CreateMany<Guid>(5).ToList();

        foreach (var id in userIds)
        {
            // Configure mock to return different results for each call
            mockEventRepository
                .GetByIdAsync(id, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Glyloop.Domain.Aggregates.Event.Event?>(null));
        }

        // Act
        var tasks = userIds.Select(id =>
            mockEventRepository.GetByIdAsync(id, CancellationToken.None));
        await Task.WhenAll(tasks);

        // Assert
        Assert.That(userIds, Has.Count.EqualTo(5));
        await mockEventRepository.Received(5)
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Example of parameterized async test with proper mock isolation
    /// </summary>
    [TestCase("2025-01-01")]
    [TestCase("2025-06-15")]
    [TestCase("2025-12-31")]
    public async Task Handle_ShouldProcessDifferentDates_WhenTimeProviderReturnsVariousDates(string dateString)
    {
        // Arrange
        var mockTimeProvider = Substitute.For<ITimeProvider>();
        var date = DateTimeOffset.Parse(dateString);
        mockTimeProvider.UtcNow.Returns(date);

        // Act
        var actualDate = mockTimeProvider.UtcNow;
        await Task.CompletedTask; // Simulate async operation

        // Assert
        Assert.That(actualDate, Is.EqualTo(date));
    }

    /// <summary>
    /// Example of testing with cancellation token - properly isolated
    /// </summary>
    [Test]
    public void Handle_ShouldRespectCancellation_WhenTokenIsCancelled()
    {
        // Arrange
        var mockEventRepository = Substitute.For<IEventRepository>();
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        mockEventRepository
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(x =>
            {
                var token = x.ArgAt<CancellationToken>(1);
                token.ThrowIfCancellationRequested();
                return Task.FromResult<Glyloop.Domain.Aggregates.Event.Event?>(null);
            });

        // Act & Assert
        Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await mockEventRepository.GetByIdAsync(Guid.NewGuid(), cancellationTokenSource.Token));
    }
}

