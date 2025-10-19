namespace Glyloop.Domain.Enums;

/// <summary>
/// Represents the type of user event logged in the system.
/// Each type has specific details and business rules.
/// Reference: DDD Plan Section 2 - Aggregates (Event)
/// </summary>
public enum EventType
{
    /// <summary>
    /// Food/meal consumption event with carbohydrate data
    /// </summary>
    Food = 1,

    /// <summary>
    /// Insulin administration event
    /// </summary>
    Insulin = 2,

    /// <summary>
    /// Exercise/physical activity event
    /// </summary>
    Exercise = 3,

    /// <summary>
    /// General note or annotation event
    /// </summary>
    Note = 4
}

