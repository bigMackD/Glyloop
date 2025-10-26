using Glyloop.API.Contracts.Account;
using Glyloop.API.Contracts.Auth;
using Glyloop.API.Contracts.Chart;
using Glyloop.API.Contracts.Common;
using Glyloop.API.Contracts.Dexcom;
using Glyloop.API.Contracts.Events;
using Glyloop.Application.DTOs.Account;
using Glyloop.Application.DTOs.Chart;
using Glyloop.Application.DTOs.Common;
using Glyloop.Application.DTOs.DexcomLink;
using Glyloop.Application.DTOs.Events;

namespace Glyloop.API.Mapping;

/// <summary>
/// Maps application layer DTOs to API response contracts.
/// All methods are pure functions with no side effects.
/// </summary>
public static class ResponseMapper
{
    #region Auth Responses

    public static RegisterResponse ToResponse(this UserRegisteredDto dto)
    {
        return new RegisterResponse(
            UserId: dto.UserId,
            Email: dto.Email,
            RegisteredAt: dto.CreatedAt);
    }

    public static LoginResponse ToLoginResponse(Guid userId, string email)
    {
        return new LoginResponse(
            UserId: userId,
            Email: email);
    }

    public static RefreshResponse ToRefreshResponse(Guid userId, string email)
    {
        return new RefreshResponse(
            UserId: userId,
            Email: email,
            RefreshedAt: DateTimeOffset.UtcNow);
    }

    #endregion

    #region Account Responses

    public static PreferencesResponse ToResponse(this UserPreferencesDto dto)
    {
        return new PreferencesResponse(
            TirLowerBound: dto.TirLowerBound,
            TirUpperBound: dto.TirUpperBound);
    }

    #endregion

    #region Dexcom Responses

    public static LinkDexcomResponse ToResponse(this DexcomLinkCreatedDto dto)
    {
        return new LinkDexcomResponse(
            LinkId: dto.LinkId,
            LinkedAt: dto.CreatedAt,
            TokenExpiresAt: dto.TokenExpiresAt);
    }

    public static DexcomStatusResponse ToResponse(this DexcomLinkStatusDto dto)
    {
        return new DexcomStatusResponse(
            IsLinked: dto.IsLinked,
            LinkedAt: null, // Not available in DTO
            TokenExpiresAt: dto.TokenExpiresAt,
            LastSyncAt: dto.LastRefreshedAt);
    }

    #endregion

    #region Event Responses

    public static EventResponse ToResponse(this EventDto dto)
    {
        return dto switch
        {
            FoodEventDto food => new FoodEventResponse(
                EventId: food.EventId,
                EventType: food.EventType.ToString(),
                EventTime: food.EventTime,
                CreatedAt: food.CreatedAt,
                Note: food.Note,
                CarbohydratesGrams: food.CarbohydratesGrams,
                MealTagId: food.MealTagId,
                AbsorptionHint: food.AbsorptionHint.ToString()),

            InsulinEventDto insulin => new InsulinEventResponse(
                EventId: insulin.EventId,
                EventType: insulin.EventType.ToString(),
                EventTime: insulin.EventTime,
                CreatedAt: insulin.CreatedAt,
                Note: insulin.Note,
                InsulinType: insulin.InsulinType.ToString(),
                InsulinUnits: insulin.Units,
                Preparation: insulin.Preparation,
                Delivery: insulin.Delivery,
                Timing: insulin.Timing),

            ExerciseEventDto exercise => new ExerciseEventResponse(
                EventId: exercise.EventId,
                EventType: exercise.EventType.ToString(),
                EventTime: exercise.EventTime,
                CreatedAt: exercise.CreatedAt,
                Note: exercise.Note,
                ExerciseTypeId: exercise.ExerciseTypeId,
                DurationMinutes: exercise.DurationMinutes,
                Intensity: exercise.Intensity.ToString()),

            NoteEventDto note => new NoteEventResponse(
                EventId: note.EventId,
                EventType: note.EventType.ToString(),
                EventTime: note.EventTime,
                CreatedAt: note.CreatedAt,
                NoteText: note.Text),

            _ => new EventResponse(
                EventId: dto.EventId,
                EventType: dto.EventType.ToString(),
                EventTime: dto.EventTime,
                CreatedAt: dto.CreatedAt,
                Note: dto.Note)
        };
    }

    public static EventListItemResponse ToListItemResponse(this EventListItemDto dto)
    {
        return new EventListItemResponse(
            EventId: dto.EventId,
            EventType: dto.EventType.ToString(),
            EventTime: dto.EventTime,
            Summary: dto.Summary);
    }

    public static PagedResponse<EventListItemResponse> ToPagedResponse(
        this PagedResult<EventListItemDto> pagedResult)
    {
        var items = pagedResult.Items
            .Select(dto => dto.ToListItemResponse())
            .ToList();

        return new PagedResponse<EventListItemResponse>(
            Items: items,
            Page: pagedResult.Page,
            PageSize: pagedResult.PageSize,
            TotalCount: pagedResult.TotalCount,
            TotalPages: pagedResult.TotalPages);
    }

    public static EventOutcomeResponse ToResponse(this EventOutcomeDto dto)
    {
        return new EventOutcomeResponse(
            EventId: dto.EventId,
            EventTime: dto.TargetTime,
            OutcomeTime: dto.ReadingTime ?? dto.TargetTime,
            GlucoseValue: dto.GlucoseValueMgDl,
            IsApproximate: !dto.HasReading,
            Message: dto.HasReading ? "Outcome recorded" : "No reading available");
    }

    #endregion

    #region Chart Responses

    public static ChartDataResponse ToResponse(this ChartDataDto dto)
    {
        var glucoseData = dto.GlucoseData
            .Select(g => new GlucoseDataPoint(g.Time, g.ValueMgDl))
            .ToList();

        var eventOverlays = dto.Events
            .Select(e => new EventOverlay(
                EventId: e.EventId,
                EventType: e.EventType.ToString(),
                Timestamp: e.EventTime,
                Icon: null,
                Color: null,
                Summary: e.Tooltip))
            .ToList();

        return new ChartDataResponse(
            GlucoseData: glucoseData,
            EventOverlays: eventOverlays,
            StartTime: dto.StartTime,
            EndTime: dto.EndTime);
    }

    public static TimeInRangeResponse ToResponse(this TimeInRangeDto dto)
    {
        var readingsBelowRange = dto.TotalReadings > 0 ? dto.TotalReadings - dto.InRangeCount - (dto.TotalReadings - dto.InRangeCount) : 0;
        var readingsAboveRange = dto.TotalReadings - dto.InRangeCount - readingsBelowRange;
        
        return new TimeInRangeResponse(
            TimeInRangePercentage: dto.TirPercentage ?? 0m,
            TotalReadings: dto.TotalReadings,
            ReadingsInRange: dto.InRangeCount,
            ReadingsBelowRange: readingsBelowRange,
            ReadingsAboveRange: readingsAboveRange,
            TargetLowerBound: dto.LowerBound,
            TargetUpperBound: dto.UpperBound,
            StartTime: DateTimeOffset.UtcNow.AddHours(-3), // Default to 3 hours ago
            EndTime: DateTimeOffset.UtcNow);
    }

    #endregion
}

