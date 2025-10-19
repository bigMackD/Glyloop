namespace Glyloop.Domain.Enums;

/// <summary>
/// Represents the source/origin of an event.
/// </summary>
public enum SourceType
{
    /// <summary>
    /// Event manually entered by the user via the UI
    /// </summary>
    Manual = 1,

    /// <summary>
    /// Event imported from an external system
    /// </summary>
    Imported = 2,

    /// <summary>
    /// Event automatically detected or suggested by the system
    /// </summary>
    System = 3
}

