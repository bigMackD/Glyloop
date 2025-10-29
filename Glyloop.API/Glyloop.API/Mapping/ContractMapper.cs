using System.Collections.Generic;
using Glyloop.API.Contracts.Account;
using Glyloop.API.Contracts.Auth;
using Glyloop.API.Contracts.Dexcom;
using Glyloop.API.Contracts.Events;
using Glyloop.Application.Commands.Account.Register;
using Glyloop.Application.Commands.Account.UpdatePreferences;
using Glyloop.Application.Commands.DexcomLink.LinkDexcom;
using Glyloop.Application.Commands.DexcomLink.UnlinkDexcom;
using Glyloop.Application.Commands.Events.AddExerciseEvent;
using Glyloop.Application.Commands.Events.AddFoodEvent;
using Glyloop.Application.Commands.Events.AddInsulinEvent;
using Glyloop.Application.Commands.Events.AddNoteEvent;
using Glyloop.Application.Queries.Chart.GetChartData;
using Glyloop.Application.Queries.Chart.GetTimeInRange;
using Glyloop.Application.Queries.Events.GetEventById;
using Glyloop.Application.Queries.Events.GetEventOutcome;
using Glyloop.Application.Queries.Events.ListEvents;
using Glyloop.Domain.Enums;
using Glyloop.Domain.ValueObjects;

namespace Glyloop.API.Mapping;

/// <summary>
/// Maps API contracts to application layer commands and queries.
/// All methods are pure functions with no side effects.
/// </summary>
public static class ContractMapper
{
    private static readonly HashSet<int> AllowedChartRanges = new() { 1, 3, 5, 8, 12, 24 };

    #region Auth Mappings

    public static RegisterUserCommand ToCommand(this RegisterRequest request)
    {
        return new RegisterUserCommand(
            Email: request.Email,
            Password: request.Password);
    }

    #endregion

    #region Account Mappings

    public static UpdatePreferencesCommand ToCommand(this UpdatePreferencesRequest request, Guid userId)
    {
        return new UpdatePreferencesCommand(
            LowerBound: request.TirLowerBound,
            UpperBound: request.TirUpperBound);
    }

    #endregion

    #region Dexcom Mappings

    public static LinkDexcomCommand ToCommand(this LinkDexcomRequest request)
    {
        return new LinkDexcomCommand(
            AuthorizationCode: request.AuthorizationCode);
    }

    public static UnlinkDexcomCommand ToCommand(this Guid userId)
    {
        return new UnlinkDexcomCommand(UserId.Create(userId));
    }

    #endregion

    #region Event Mappings

    public static AddFoodEventCommand ToCommand(this CreateFoodEventRequest request)
    {
        return new AddFoodEventCommand(
            EventTime: request.EventTime,
            CarbohydratesGrams: request.CarbohydratesGrams,
            MealTagId: request.MealTagId,
            AbsorptionHint: ParseAbsorptionHint(request.AbsorptionHint),
            Note: request.Note);
    }

    public static AddInsulinEventCommand ToCommand(this CreateInsulinEventRequest request)
    {
        return new AddInsulinEventCommand(
            EventTime: request.EventTime,
            InsulinType: ParseInsulinType(request.InsulinType),
            Units: request.InsulinUnits,
            Preparation: request.Preparation,
            Delivery: request.Delivery,
            Timing: request.Timing,
            Note: request.Note);
    }

    public static AddExerciseEventCommand ToCommand(this CreateExerciseEventRequest request)
    {
        return new AddExerciseEventCommand(
            EventTime: request.EventTime,
            ExerciseTypeId: request.ExerciseTypeId,
            DurationMinutes: request.DurationMinutes,
            Intensity: ParseIntensityType(request.Intensity),
            Note: request.Note);
    }

    public static AddNoteEventCommand ToCommand(this CreateNoteEventRequest request)
    {
        return new AddNoteEventCommand(
            EventTime: request.EventTime,
            Text: request.NoteText);
    }

    public static ListEventsQuery ToQuery(
        this EventType? eventType,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        int page,
        int pageSize)
    {
        return new ListEventsQuery(
            EventType: eventType,
            FromDate: fromDate,
            ToDate: toDate,
            Page: page,
            PageSize: pageSize);
    }

    public static GetEventByIdQuery ToQuery(this Guid eventId)
    {
        return new GetEventByIdQuery(EventId: eventId);
    }

    public static GetEventOutcomeQuery ToOutcomeQuery(this Guid eventId)
    {
        return new GetEventOutcomeQuery(EventId: eventId);
    }

    #endregion

    #region Chart Mappings

    public static GetChartDataQuery ToQuery(this string range)
    {
        return new GetChartDataQuery(Range: range);
    }

    public static GetTimeInRangeQuery ToTirQuery(this string range)
    {
        var (fromTime, toTime) = ParseTimeRange(range);
        return new GetTimeInRangeQuery(
            FromTime: fromTime,
            ToTime: toTime);
    }

    #endregion

    #region Helper Methods

    private static AbsorptionHint? ParseAbsorptionHint(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return value.ToLowerInvariant() switch
        {
            "rapid" => AbsorptionHint.Rapid,
            "normal" => AbsorptionHint.Normal,
            "slow" => AbsorptionHint.Slow,
            "other" => AbsorptionHint.Other,
            _ => null
        };
    }

    private static InsulinType ParseInsulinType(string value)
    {
        return value.ToLowerInvariant() switch
        {
            "fast" => InsulinType.Fast,
            "long" => InsulinType.Long,
            _ => throw new ArgumentException($"Invalid insulin type: {value}", nameof(value))
        };
    }

    private static IntensityType? ParseIntensityType(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return value.ToLowerInvariant() switch
        {
            "light" => IntensityType.Light,
            "moderate" => IntensityType.Moderate,
            "vigorous" => IntensityType.Vigorous,
            _ => null
        };
    }

    private static (DateTimeOffset FromTime, DateTimeOffset ToTime) ParseTimeRange(string range)
    {
        var now = DateTimeOffset.UtcNow;
        if (!int.TryParse(range, out var hours) || !AllowedChartRanges.Contains(hours))
        {
            hours = 3;
        }

        return (now.AddHours(-hours), now);
    }

    #endregion
}

