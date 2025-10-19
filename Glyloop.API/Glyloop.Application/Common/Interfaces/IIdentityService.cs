using Glyloop.Domain.Common;
using Glyloop.Domain.ValueObjects;

namespace Glyloop.Application.Common.Interfaces;

/// <summary>
/// Service for managing user identity operations (registration, authentication).
/// Abstracts ASP.NET Core Identity to keep Application layer independent.
/// </summary>
public interface IIdentityService
{
    /// <summary>
    /// Creates a new user account with email and password.
    /// </summary>
    /// <param name="email">User's email address</param>
    /// <param name="password">User's password (will be hashed)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the created user's ID or error</returns>
    Task<Result<Guid>> CreateUserAsync(string email, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an email address is already registered.
    /// </summary>
    /// <param name="email">Email address to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if email exists, false otherwise</returns>
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets user's Time in Range preferences.
    /// </summary>
    /// <param name="userId">User's unique identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing TirRange or error if user not found</returns>
    Task<Result<TirRange>> GetUserPreferencesAsync(UserId userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates user's Time in Range preferences.
    /// </summary>
    /// <param name="userId">User's unique identifier</param>
    /// <param name="tirRange">New TIR range preferences</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating success or failure</returns>
    Task<Result> UpdateUserPreferencesAsync(UserId userId, TirRange tirRange, CancellationToken cancellationToken = default);
}

