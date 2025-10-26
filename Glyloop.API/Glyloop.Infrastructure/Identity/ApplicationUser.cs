using Microsoft.AspNetCore.Identity;

namespace Glyloop.Infrastructure.Identity;

/// <summary>
/// Custom user entity extending ASP.NET Core Identity with Guid primary key.
/// Represents an authenticated user in the system.
/// 
/// Infrastructure mapping notes:
/// - UserId maps to the domain's UserId value object (Guid wrapper)
/// - Navigation properties are for Infrastructure queries only (not exposed to Domain)
/// - All timestamps stored as timestamptz in PostgreSQL
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    /// <summary>
    /// Gets or sets when this user account was created (UTC).
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the last time this user logged in (UTC).
    /// </summary>
    public DateTimeOffset? LastLoginAt { get; set; }

    /// <summary>
    /// Gets or sets the lower bound of the Time in Range (TIR) target in mg/dL.
    /// Default: 70 mg/dL
    /// </summary>
    public int TirLowerBound { get; set; } = 70;

    /// <summary>
    /// Gets or sets the upper bound of the Time in Range (TIR) target in mg/dL.
    /// Default: 180 mg/dL
    /// </summary>
    public int TirUpperBound { get; set; } = 180;
}

