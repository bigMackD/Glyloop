using System.ComponentModel.DataAnnotations;

namespace Glyloop.API.Contracts.Events;

/// <summary>
/// Request to log a free-text note event.
/// </summary>
public record CreateNoteEventRequest
{
    /// <summary>
    /// Time of the note. Cannot be in the future.
    /// </summary>
    [Required]
    public required DateTimeOffset EventTime { get; init; }

    /// <summary>
    /// Note text (1-500 characters).
    /// </summary>
    [Required]
    [MinLength(1)]
    [MaxLength(500, ErrorMessage = "Note must be between 1 and 500 characters")]
    public required string NoteText { get; init; }
}

