namespace Glyloop.Domain.Enums;

/// <summary>
/// Represents the intensity level of an exercise session.
/// Reference: DDD Plan Section 3 - Value Objects
/// </summary>
public enum IntensityType
{
    /// <summary>
    /// Light intensity (e.g., casual walking, stretching)
    /// </summary>
    Light = 1,

    /// <summary>
    /// Moderate intensity (e.g., brisk walking, recreational cycling)
    /// </summary>
    Moderate = 2,

    /// <summary>
    /// Vigorous intensity (e.g., running, competitive sports)
    /// </summary>
    Vigorous = 3
}

