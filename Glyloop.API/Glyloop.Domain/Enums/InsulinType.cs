namespace Glyloop.Domain.Enums;

/// <summary>
/// Represents the type of insulin administered.
/// Reference: DDD Plan Section 3 - Value Objects
/// </summary>
public enum InsulinType
{
    /// <summary>
    /// Fast-acting (bolus) insulin for meal coverage
    /// </summary>
    Fast = 1,

    /// <summary>
    /// Long-acting (basal) insulin for background coverage
    /// </summary>
    Long = 2
}

