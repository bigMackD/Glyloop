namespace Glyloop.Domain.Common;

/// <summary>
/// Abstraction for providing the current time.
/// Injected to enable deterministic testing of time-dependent behavior.
/// </summary>
public interface ITimeProvider
{
    /// <summary>
    /// Gets the current UTC date and time.
    /// </summary>
    DateTimeOffset UtcNow { get; }
}

