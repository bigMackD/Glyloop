using Glyloop.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Glyloop.Infrastructure.Persistence.Converters;

/// <summary>
/// Reusable EF Core value converters for domain Value Objects.
/// These converters handle the transformation between domain VOs and database primitives.
/// </summary>
public static class ValueObjectConverters
{
    /// <summary>
    /// Converts UserId value object to/from Guid for database storage.
    /// </summary>
    public static ValueConverter<UserId, Guid> UserIdConverter { get; } =
        new ValueConverter<UserId, Guid>(
            vo => vo.Value,
            guid => UserId.Create(guid));

    /// <summary>
    /// Converts Carbohydrate value object to/from int (grams) for database storage.
    /// </summary>
    public static ValueConverter<Carbohydrate, int> CarbohydrateConverter { get; } =
        new ValueConverter<Carbohydrate, int>(
            vo => vo.Grams,
            grams => Carbohydrate.Create(grams).Value);

    /// <summary>
    /// Converts InsulinDose value object to/from decimal (units) for database storage.
    /// </summary>
    public static ValueConverter<InsulinDose, decimal> InsulinDoseConverter { get; } =
        new ValueConverter<InsulinDose, decimal>(
            vo => vo.Units,
            units => InsulinDose.Create(units).Value);

    /// <summary>
    /// Converts ExerciseDuration value object to/from int (minutes) for database storage.
    /// </summary>
    public static ValueConverter<ExerciseDuration, int> ExerciseDurationConverter { get; } =
        new ValueConverter<ExerciseDuration, int>(
            vo => vo.Minutes,
            minutes => ExerciseDuration.Create(minutes).Value);

    /// <summary>
    /// Converts NoteText value object to/from string for database storage.
    /// </summary>
    public static ValueConverter<NoteText, string> NoteTextConverter { get; } =
        new ValueConverter<NoteText, string>(
            vo => vo.Text,
            text => NoteText.Create(text).Value);

    /// <summary>
    /// Converts nullable NoteText value object to/from nullable string for database storage.
    /// </summary>
    public static ValueConverter<NoteText?, string?> NullableNoteTextConverter { get; } =
        new ValueConverter<NoteText?, string?>(
            vo => vo != null ? vo.Text : null,
            text => text != null ? NoteText.Create(text).Value : null);

    /// <summary>
    /// Converts MealTagId value object to/from int for database storage.
    /// </summary>
    public static ValueConverter<MealTagId, int> MealTagIdConverter { get; } =
        new ValueConverter<MealTagId, int>(
            vo => vo.Value,
            value => MealTagId.Create(value));

    /// <summary>
    /// Converts ExerciseTypeId value object to/from int for database storage.
    /// </summary>
    public static ValueConverter<ExerciseTypeId, int> ExerciseTypeIdConverter { get; } =
        new ValueConverter<ExerciseTypeId, int>(
            vo => vo.Value,
            value => ExerciseTypeId.Create(value));
}

