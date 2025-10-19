using Glyloop.Domain.Common;

namespace Glyloop.Infrastructure.Services;

/// <summary>
/// Production implementation of ITimeProvider that returns real system time.
/// Implements the domain's ITimeProvider interface to provide current UTC time.
/// 
/// Purpose:
/// - Provides dependency injection point for time in domain layer
/// - Enables deterministic testing by allowing time to be faked in tests
/// - Always returns UTC time to ensure consistency across timezones
/// </summary>
public class SystemTimeProvider : ITimeProvider
{
    /// <inheritdoc/>
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

/// <summary>
/// Fake time provider for testing purposes.
/// Allows tests to control the current time for deterministic behavior.
/// </summary>
public class FakeTimeProvider : ITimeProvider
{
    /// <summary>
    /// Gets or sets the fake current time.
    /// Tests can set this to any value to simulate different time scenarios.
    /// </summary>
    public DateTimeOffset UtcNow { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Advances the fake time by the specified timespan.
    /// </summary>
    public void Advance(TimeSpan timespan)
    {
        UtcNow = UtcNow.Add(timespan);
    }

    /// <summary>
    /// Sets the fake time to a specific value.
    /// </summary>
    public void SetTime(DateTimeOffset time)
    {
        UtcNow = time;
    }
}

