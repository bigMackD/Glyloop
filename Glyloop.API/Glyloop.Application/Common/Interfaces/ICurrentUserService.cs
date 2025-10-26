namespace Glyloop.Application.Common.Interfaces;

/// <summary>
/// Service for accessing the current authenticated user's information.
/// Implemented by the API layer using HttpContext to extract user claims from JWT.
/// 
/// Note: The API layer guarantees authentication via [Authorize] attribute,
/// so UserId is always available when handlers execute.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Gets the current authenticated user's unique identifier from JWT claims.
    /// This value is guaranteed to be present because the API layer enforces
    /// authentication via [Authorize] attribute before handlers execute.
    /// </summary>
    Guid UserId { get; }
}

