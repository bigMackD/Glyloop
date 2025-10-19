using Glyloop.Domain.Common;
using Glyloop.Domain.Errors;

namespace Glyloop.Domain.ValueObjects;

/// <summary>
/// Represents text content for notes or annotations.
/// Invariant: Must be between 1 and 500 characters.
/// Reference: DDD Plan Section 3 - Value Objects
/// </summary>
public sealed class NoteText : ValueObject
{
    public string Text { get; }

    private NoteText(string text)
    {
        Text = text;
    }

    /// <summary>
    /// Creates a note text value object with validation.
    /// </summary>
    /// <param name="text">Note text (1-500 characters)</param>
    public static Result<NoteText> Create(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Result.Failure<NoteText>(DomainErrors.Event.InvalidNoteText);

        var trimmed = text.Trim();
        if (trimmed.Length < 1 || trimmed.Length > 500)
            return Result.Failure<NoteText>(DomainErrors.Event.InvalidNoteText);

        return Result.Success(new NoteText(trimmed));
    }

    /// <summary>
    /// Creates an optional note text, returning null for empty input.
    /// </summary>
    public static NoteText? CreateOptional(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var result = Create(text);
        return result.IsSuccess ? result.Value : null;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Text;
    }

    public override string ToString() => Text;

    public static implicit operator string(NoteText noteText) => noteText.Text;
}

