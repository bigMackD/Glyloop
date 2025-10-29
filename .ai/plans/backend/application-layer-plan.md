# Glyloop Application Layer - Implementation Plan

## 1. Overview

### Purpose
The Application Layer orchestrates business workflows using CQRS, coordinating command execution, query execution, input validation, DTO mapping, and transaction management.

### Architecture Patterns
- **CQRS**: Separate read and write operations
- **Mediator**: MediatR for decoupled request/response handling
- **Pipeline Behaviors**: Validation, logging, authorization
- **Result Pattern**: Explicit error handling with `Result<T>`

### Technology Stack
- .NET 8, MediatR 12.x, FluentValidation 11.x
- Manual DTO mapping (no AutoMapper)

---

## 2. Project Structure

```
Glyloop.Application/
├── Commands/
│   ├── Account/
│   │   ├── Register/
│   │   │   ├── RegisterUserCommand.cs
│   │   │   ├── RegisterUserCommandHandler.cs
│   │   │   └── RegisterUserCommandValidator.cs
│   │   └── UpdatePreferences/
│   │       ├── UpdatePreferencesCommand.cs
│   │       ├── UpdatePreferencesCommandHandler.cs
│   │       └── UpdatePreferencesCommandValidator.cs
│   ├── DexcomLink/
│   │   ├── LinkDexcom/
│   │   ├── UnlinkDexcom/
│   │   └── RefreshDexcomToken/
│   └── Events/
│       ├── AddFoodEvent/
│       ├── AddInsulinEvent/
│       ├── AddExerciseEvent/
│       └── AddNoteEvent/
├── Queries/
│   ├── Account/
│   │   └── GetUserPreferences/
│   ├── DexcomLink/
│   │   ├── GetDexcomLinkStatus/
│   │   └── GetDexcomLinks/
│   ├── Events/
│   │   ├── GetEventById/
│   │   ├── ListEvents/
│   │   └── GetEventOutcome/
│   └── Chart/
│       ├── GetChartData/
│       └── GetTimeInRange/
├── DTOs/
│   ├── Account/
│   ├── DexcomLink/
│   ├── Events/
│   ├── Chart/
│   └── Common/
├── Validators/
│   ├── ValueObjects/
│   └── Common/
├── Common/
│   ├── Behaviors/
│   │   ├── ValidationBehavior.cs
│   │   └── LoggingBehavior.cs
│   ├── Interfaces/
│   │   ├── ICurrentUserService.cs
│   │   ├── IIdentityService.cs
│   │   ├── IDexcomService.cs
│   │   ├── ITokenEncryptionService.cs
│   │   └── IUnitOfWork.cs
│   ├── Exceptions/
│   │   └── ApplicationException.cs
│   └── Extensions/
│       └── ResultExtensions.cs
└── DependencyInjection.cs
```

---

## 3. Commands Summary

### Account Context
| Command | Purpose | Parameters | Key Validations |
|---------|---------|------------|-----------------|
| `RegisterUserCommand` | Create new user account | Email, Password | Email format, Password ≥12 chars |
| `UpdatePreferencesCommand` | Update TIR range | LowerBound, UpperBound | Lower < Upper, both 0-1000 |

### DexcomLink Context
| Command | Purpose | Parameters | Key Validations |
|---------|---------|------------|-----------------|
| `LinkDexcomCommand` | Exchange OAuth code for tokens | AuthorizationCode, CodeVerifier | NonEmpty, valid PKCE |
| `UnlinkDexcomCommand` | Disconnect Dexcom integration | LinkId, PurgeData | User owns link |
| `RefreshDexcomTokenCommand` | Refresh expired tokens (post-MVP) | LinkId | Token near expiry |

### Event Context
| Command | Purpose | Parameters | Key Validations |
|---------|---------|------------|-----------------|
| `AddFoodEventCommand` | Log food intake | EventTime, CarbohydratesGrams, MealTagId?, AbsorptionHint?, Note? | Carbs 0-300g, EventTime ≤ now, Note ≤500 chars |
| `AddInsulinEventCommand` | Log insulin dose | EventTime, InsulinType, Units, Preparation?, Delivery?, Timing?, Note? | Units 0-100 in 0.5 increments, EventTime ≤ now |
| `AddExerciseEventCommand` | Log exercise activity | EventTime, ExerciseTypeId, DurationMinutes, Intensity? | Duration 1-300 min, EventTime ≤ now |
| `AddNoteEventCommand` | Log free-text note | EventTime, Text | Text 1-500 chars, EventTime ≤ now |

### Handler Pattern
All command handlers follow this pattern:
1. Inject dependencies (Repository, UnitOfWork, CurrentUserService, TimeProvider)
2. Create/validate value objects from command parameters
3. Create or load aggregate
4. Perform business operation
5. Persist via `_unitOfWork.SaveEntitiesAsync()` (dispatches domain events)
6. Map aggregate to DTO and return `Result<T>`

---

## 4. Queries Summary

### Account Context
| Query | Purpose | Parameters | Returns |
|-------|---------|------------|---------|
| `GetUserPreferencesQuery` | Get user's TIR range | None | UserPreferencesDto |

### DexcomLink Context
| Query | Purpose | Parameters | Returns |
|-------|---------|------------|---------|
| `GetDexcomLinkStatusQuery` | Check active connection | None | DexcomLinkStatusDto |
| `GetDexcomLinksQuery` | List all user links | None | List<DexcomLinkDto> |

### Event Context
| Query | Purpose | Parameters | Returns | Validations |
|-------|---------|------------|---------|-------------|
| `ListEventsQuery` | Get event history with filters | EventType?, FromDate?, ToDate?, Page, PageSize | PagedResult<EventListItemDto> | Page ≥1, PageSize 1-100, FromDate ≤ ToDate |
| `GetEventByIdQuery` | Get event details | EventId | EventDetailDto | User owns event |
| `GetEventOutcomeQuery` | Get +2h glucose for food event | EventId | EventOutcomeDto | Must be food event, user owns |

### Chart Context
| Query | Purpose | Parameters | Returns | Validations |
|-------|---------|------------|---------|-------------|
| `GetChartDataQuery` | Fetch glucose + events for range | Range ("1h", "3h", etc.) | ChartDataDto | Range in valid list |
| `GetTimeInRangeQuery` | Calculate TIR % | FromTime, ToTime | TimeInRangeDto | FromTime < ToTime |

### Handler Pattern
All query handlers follow this pattern:
1. Inject dependencies (Repositories, CurrentUserService)
2. Validate authorization (user can only query their own data)
3. Query repository/read model with filters
4. Map entities to DTOs
5. Return `Result<T>`

**Performance Notes**:
- Use `AsNoTracking()` for read-only queries
- Ensure indexes on `(UserId, EventTime)` for Events and `(UserId, SystemTime)` for GlucoseReadings
- Consider parallelizing multiple repository calls (e.g., glucose + events in chart query)
- Consider caching for frequently accessed data (chart ranges, user preferences)

---

## 5. DTOs Catalog

### Account DTOs
- `RegisterRequestDto`, `UserRegisteredDto`
- `LoginRequestDto`, `LoginResponseDto`
- `UpdatePreferencesRequestDto`, `UserPreferencesDto`

### DexcomLink DTOs
- `LinkDexcomRequestDto`, `DexcomLinkCreatedDto`
- `UnlinkDexcomRequestDto`
- `DexcomLinkStatusDto`

### Event DTOs
**Base**: `EventDto` (abstract with Id, UserId, EventType, EventTime, CreatedAt, Note)

**Specific Types**:
- `FoodEventDto` extends EventDto → adds CarbohydratesGrams, MealTagId?, AbsorptionHint?
- `InsulinEventDto` extends EventDto → adds InsulinType, Units, Preparation?, Delivery?, Timing?
- `ExerciseEventDto` extends EventDto → adds ExerciseTypeId, DurationMinutes, Intensity?
- `NoteEventDto` extends EventDto → adds Text

**Summary**:
- `EventListItemDto` (Id, EventType, EventTime, Summary)
- `EventOutcomeDto` (EventId, TargetTime, GlucoseValue?, ReadingTime?, HasReading)

### Chart DTOs
- `ChartDataDto` (StartTime, EndTime, GlucoseData[], Events[])
- `GlucoseDataPointDto` (Time, ValueMgDl, Trend?)
- `EventOverlayDto` (EventId, EventType, EventTime, Icon, Color, Tooltip)
- `TimeInRangeDto` (TirPercentage?, TotalReadings, InRangeCount, LowerBound, UpperBound)

### Common DTOs
- `PagedResult<T>` (Items[], TotalCount, Page, PageSize, TotalPages, HasNextPage, HasPreviousPage)
- `ErrorDto` (Code, Message, ValidationErrors?)

---

## 6. Validation Rules Summary

### Value Object Validators
| Validator | Rule | Message |
|-----------|------|---------|
| Carbohydrate | 0-300 g | "Carbohydrates must be between 0 and 300 grams" |
| InsulinDose | 0-100 U in 0.5 increments | "Insulin dose must be between 0 and 100 units in 0.5 increments" |
| ExerciseDuration | 1-300 minutes | "Exercise duration must be between 1 and 300 minutes" |
| NoteText | 1-500 chars | "Note must be between 1 and 500 characters" |

### Common Patterns
- **Cross-field**: Use `.Must()` with lambda for complex conditions (e.g., LowerBound < UpperBound)
- **Conditional**: Use `.When()` to apply rules only if condition met (e.g., validate Note only if not empty)
- **Async**: Use `.MustAsync()` for database checks (e.g., email uniqueness)

---

## 7. Pipeline Behaviors

### ValidationBehavior
- **Purpose**: Auto-validate all requests before handler execution
- **Logic**: Run all registered validators, collect failures, return `Result.Failure` with validation errors if any

### LoggingBehavior
- **Purpose**: Log request start/end with timing
- **Logic**: Log request name + userId, execute handler with stopwatch, log duration (or error)

**Pipeline Order** (order matters!):
1. LoggingBehavior (outermost - logs everything)
2. ValidationBehavior (validates before processing)

**Note**: Authentication is handled at the API layer via ASP.NET Core's `[Authorize]` attribute, not in Application layer behaviors. This provides cleaner separation of concerns and eliminates boilerplate authentication checks in handlers.

---

## 8. Dependency Injection

**File**: `DependencyInjection.cs`

```csharp
public static IServiceCollection AddApplication(this IServiceCollection services)
{
    var assembly = typeof(DependencyInjection).Assembly;
    
    // MediatR with behaviors (order matters!)
    services.AddMediatR(config => {
        config.RegisterServicesFromAssembly(assembly);
        config.AddOpenBehavior(typeof(LoggingBehavior<,>));
        config.AddOpenBehavior(typeof(ValidationBehavior<,>));
    });
    
    // FluentValidation
    services.AddValidatorsFromAssembly(assembly);
    
    return services;
}
```

**Usage**: `builder.Services.AddApplication().AddInfrastructure(config).AddPresentation();`

## 9. Integration Patterns

### With Infrastructure
- **Repositories**: Inject `IEventRepository`, `IDexcomLinkRepository`, etc. Use in handlers, persist via `IUnitOfWork.SaveEntitiesAsync()`
- **External Services**: Inject `IDexcomApiClient`, `ITokenEncryptionService`. Handle `Result<T>` returns from external calls
- **Identity**: Inject `UserManager<ApplicationUser>`. Use for user creation, lookup, and updates

### With API Layer
- **Controllers**: Inject `ISender` (MediatR), send commands/queries, map `Result<T>` to HTTP responses
- **Error Mapping**: Validation → 400, NotFound → 404, Forbidden → 403, Conflict → 409, else → 500
- **Authentication**: Use `[Authorize]` attribute on controllers/endpoints to enforce authentication
  - Anonymous endpoints (Login, Register) use `[AllowAnonymous]`
  - `ICurrentUserService` provides current user ID extracted from JWT claims in `HttpContext`
  - Application layer assumes authentication is already validated (no boilerplate checks needed)

---

## 10. Testing Strategy

### Unit Tests
**Focus**: Command/Query handlers with mocked dependencies

**Setup**:
- Mock repositories, UnitOfWork, CurrentUserService, TimeProvider
- Test success paths (valid commands return success)
- Test failure paths (invalid data returns domain errors)
- Verify repository methods called correctly

### Validation Tests
**Focus**: FluentValidation rules

**Approach**:
- Use `[Theory]` with `[InlineData]` for boundary testing
- Test each validation rule independently
- Test conditional validations (`.When()`)
- Test async validations (email uniqueness, etc.)

### Integration Tests
**Focus**: Full flow from HTTP request to database

**Setup**:
- Use `WebApplicationFactory<Program>` for test server
- Test database with apply migrations
- Authenticate requests with test JWT
- Verify database state after commands
- Test error responses (400, 401, 403, 404)

---

## 11. Implementation Phases

| Phase | Goal | Key Deliverables | Duration |
|-------|------|-----------------|----------|
| **1. Foundation** | Setup infrastructure | Project, DI, Behaviors, Test setup | 1-2 days |
| **2. Account** | User registration & preferences | Register/UpdatePreferences commands, GetPreferences query | 2-3 days |
| **3. DexcomLink** | OAuth integration | Link/Unlink commands, GetStatus query, token handling | 2-3 days |
| **4. Events - Write** | Event creation | AddFood/Insulin/Exercise/Note commands with validation | 3-4 days |
| **5. Events - Read** | Event queries | ListEvents, GetEventById, GetEventOutcome queries | 2-3 days |
| **6. Chart & TIR** | Visualization data | GetChartData, GetTimeInRange queries | 3-4 days |
| **7. Polish** | Integration & testing | Error handling, auth checks, E2E tests | 2-3 days |

**Total**: 15-22 days

---

## 12. Key Design Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| DTO Mapping | Manual mapping | Explicit, fewer dependencies, simple enough for MVP |
| Read Models Location | Infrastructure Layer | Keeps Application focused on orchestration |
| Chart Data Aggregation | In query handler | Simple enough for MVP; extract to service if complex |
| Caching | None in MVP | Focus on indexing first; add Redis post-MVP if needed |
| Concurrent Updates | Not handled | Events immutable, preferences last-write-wins acceptable |
| Dexcom API Errors | Return domain errors | API layer maps to HTTP status; retry in Infrastructure (Polly) |
| Event Mutability | Immutable in MVP | No UpdatedAt; add audit log post-MVP if editing added |

---

## 13. Deliverables Summary

**Commands** (9 total): Register, UpdatePreferences, Link/UnlinkDexcom, AddFood/Insulin/Exercise/Note + handlers + validators

**Queries** (8 total): GetPreferences, GetDexcomLinkStatus/Links, ListEvents, GetEventById, GetEventOutcome, GetChartData, GetTimeInRange + handlers + validators (where needed)

**DTOs** (~20 total): Grouped by Account, DexcomLink, Event (base + 4 subtypes), Chart, Common (PagedResult, ErrorDto)

**Infrastructure** (8 items): ICurrentUserService (non-nullable), IIdentityService, IDexcomService, ITokenEncryptionService, IUnitOfWork, ValidationBehavior, LoggingBehavior, DependencyInjection.cs

**Tests** (3 categories): Unit tests for handlers, Validation tests for rules, Integration tests for critical flows

**Test Location**: `/Glyloop.API/Tests/Glyloop.Tests.Application/`

---

## 14. Success Criteria

**MVP Complete When**:
- ✅ All 17 PRD user stories supported
- ✅ All commands persist correctly via UnitOfWork
- ✅ All queries return data with user isolation
- ✅ Validation prevents invalid input (PRD constraints enforced)
- ✅ Domain events dispatched on successful persistence
- ✅ Authorization checks prevent cross-user data access
- ✅ Result<T> pattern used consistently
- ✅ Error handling maps correctly to HTTP status codes
- ✅ Test coverage ≥70% for handlers and validators
- ✅ Performance acceptable (<200ms for complex queries)

---

## 15. Next Steps

1. Create project: `dotnet new classlib -n Glyloop.Application`
2. Install packages: MediatR, FluentValidation, FluentValidation.DependencyInjectionExtensions
3. Set up folder structure (Section 2)
4. Implement Phase 1 (Foundation: DI, behaviors, interfaces)
5. Iterate through phases 2-7, testing incrementally

---

**End of Application Layer Implementation Plan**

This plan provides a structured approach to implementing the Application Layer with CQRS, MediatR, and FluentValidation. Follow the phase-by-phase approach for incremental MVP delivery.

