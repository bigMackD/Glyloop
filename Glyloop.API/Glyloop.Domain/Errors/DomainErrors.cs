using Glyloop.Domain.Common;

namespace Glyloop.Domain.Errors;

/// <summary>
/// Centralized domain error definitions organized by concern.
/// Each error has a unique code and descriptive message.
/// </summary>
public static class DomainErrors
{
    public static class User
    {
        public static Error InvalidEmail => Error.Create(
            "User.InvalidEmail",
            "The email address format is invalid.");

        public static Error InvalidTirRange => Error.Create(
            "User.InvalidTirRange",
            "TIR range lower bound must be less than upper bound, and both must be between 0 and 1000.");
    }

    public static class DexcomLink
    {
        public static Error TokenExpired => Error.Create(
            "DexcomLink.TokenExpired",
            "The Dexcom access token has expired.");

        public static Error InvalidAuthorizationCode => Error.Create(
            "DexcomLink.InvalidAuthorizationCode",
            "The authorization code cannot be empty.");

        public static Error LinkNotFound => Error.Create(
            "DexcomLink.LinkNotFound",
            "The Dexcom link was not found.");
    }

    public static class Event
    {
        public static Error EventInFuture => Error.Create(
            "Event.EventInFuture",
            "Event time cannot be in the future.");

        public static Error InvalidCarbohydrates => Error.Create(
            "Event.InvalidCarbohydrates",
            "Carbohydrates must be between 0 and 300 grams.");

        public static Error InvalidInsulinDose => Error.Create(
            "Event.InvalidInsulinDose",
            "Insulin dose must be between 0 and 100 units in 0.5 increments.");

        public static Error InvalidExerciseDuration => Error.Create(
            "Event.InvalidExerciseDuration",
            "Exercise duration must be between 1 and 300 minutes.");

        public static Error InvalidNoteText => Error.Create(
            "Event.InvalidNoteText",
            "Note text must be between 1 and 500 characters.");

        public static Error EventImmutable => Error.Create(
            "Event.EventImmutable",
            "Events are immutable and cannot be modified after creation.");
    }
}

