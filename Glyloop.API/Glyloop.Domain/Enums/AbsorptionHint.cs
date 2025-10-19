namespace Glyloop.Domain.Enums;

/// <summary>
/// Indicates the expected absorption rate of consumed food.
/// Used to provide context for insulin dosing and glucose prediction.
/// Reference: DDD Plan Section 3 - Value Objects
/// </summary>
public enum AbsorptionHint
{
    /// <summary>
    /// Rapid absorption (e.g., simple sugars, glucose tablets)
    /// </summary>
    Rapid = 1,

    /// <summary>
    /// Normal absorption (e.g., balanced meals)
    /// </summary>
    Normal = 2,

    /// <summary>
    /// Slow absorption (e.g., high-fat or high-fiber meals)
    /// </summary>
    Slow = 3,

    /// <summary>
    /// Other or mixed absorption patterns
    /// </summary>
    Other = 4
}

