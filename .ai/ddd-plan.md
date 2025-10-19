# DOMAIN DRIVEN DESIGN Plan

## 1. Bounded contexts
- **Account**: Manages user registration, authentication, and preferences (TIR range).
- **DexcomLink**: Manages linking/unlinking Dexcom Sandbox accounts and token lifecycle.
- **Event**: Manages creation of immutable domain-specific events (Food, Insulin, Exercise, Note) and their read-model behaviors.

## 2. Aggregates

### Account
- **Aggregate Root**: User
- Properties:
  - `UserId: Guid`
  - `Email: EmailAddress`
  - `PasswordHash: string`
  - `Preferences: Preference` (holds TIRRange)
- Invariants:
  - Email must be unique
  - Password length ≥ 12
  - TIRRange.Lower < TIRRange.Upper

### DexcomLink
- **Aggregate Root**: DexcomLink
- Properties:
  - `LinkId: Guid`
  - `UserId: Guid`
  - `EncryptedAccessToken: byte[]`
  - `EncryptedRefreshToken: byte[]`
  - `TokenExpiresAt: DateTimeOffset`
  - `LastRefreshedAt: DateTimeOffset`
- Invariants:
  - TokenExpiresAt > now
  - Refresh operations follow rotation policy

### Event
- **Aggregate Root**: Event
- Properties:
  - `EventId: Guid`
  - `UserId: Guid`
  - `EventTime: DateTimeOffset`
  - `Type: EventType` (enum)
  - `Source: SourceType` (enum)
  - Subtype details (one of FoodDetails, InsulinDetails, ExerciseDetails, NoteDetails)
- Invariants:
  - EventTime must be ≤ now
  - Subtype-specific constraints (e.g., carbs_g between 0–300)
  - Immutable once persisted

## 3. Value objects
- **EmailAddress**: single property `Value: string`, valid email format
- **Password**: single property `Value: string`, hashed, min length constraint
- **TIRRange**: `Lower: int`, `Upper: int`, invariant `Lower < Upper`
- **Carbohydrate**: `Grams: int` (0–300)
- **InsulinDose**: `Units: decimal` (0–100, 0.5 increments)
- **MealTagId**: `int`
- **AbsorptionHint**: enum {Rapid, Normal, Slow, Other}
- **ExerciseTypeId**: `int`
- **ExerciseDuration**: `Minutes: int` (1–300)
- **IntensityType**: enum {Light, Moderate, Vigorous}
- **NoteText**: `Text: string` (max 500 chars)
- **EventTime**: `DateTimeOffset`
- **InsulinType**: enum {Fast, Long}

## 4. Domain events
- `UserRegistered(UserId, EmailAddress)`
- `PreferenceUpdated(UserId, TIRRange)`
- `DexcomLinked(UserId, LinkId)`
- `DexcomUnlinked(UserId, LinkId)`
- `FoodEventCreated(EventId, UserId, Carbohydrate, MealTagId, AbsorptionHint, NoteText, EventTime)`
- `InsulinEventCreated(EventId, UserId, InsulinType, InsulinDose, Preparation, Delivery, Timing, NoteText, EventTime)`
- `ExerciseEventCreated(EventId, UserId, ExerciseTypeId, ExerciseDuration, IntensityType, EventTime)`
- `NoteEventCreated(EventId, UserId, NoteText, EventTime)`

## 5. Commands

### Account Context

#### RegisterUserCommand
- **Parameters**: `Email: string`, `Password: string`
- **Flow**: HTTP POST `/api/user/register` → `RegisterUserCommandHandler` → validate → create User aggregate → raise `UserRegistered` → persist

#### LoginUserCommand
- **Parameters**: `Email: string`, `Password: string`
- **Flow**: HTTP POST `/api/user/login` → `LoginUserCommandHandler` → validate credentials → issue JWT + cookie

#### UpdatePreferenceCommand
- **Parameters**: `UserId: Guid`, `Lower: int`, `Upper: int`
- **Flow**: HTTP PUT `/api/user/preference` → `UpdatePreferenceCommandHandler` → load User aggregate → validate TIRRange → update Preferences → raise `PreferenceUpdated` → persist

### DexcomLink Context

#### LinkDexcomCommand
- **Parameters**: `UserId: Guid`, `AuthorizationCode: string`
- **Flow**: HTTP POST `/api/dexcom-link` → `LinkDexcomCommandHandler` → exchange `authorizationCode`, `client_secret` and `redirect_uri` for tokens via Dexcom `/oauth2/token` endpoint → create DexcomLink aggregate → raise `DexcomLinked` → persist

#### UnlinkDexcomCommand
- **Parameters**: `UserId: Guid`, `LinkId: Guid`, `PurgeData: bool` (optional)
- **Flow**: HTTP DELETE `/api/dexcom-link/{linkId}` → `UnlinkDexcomCommandHandler` → mark or delete link → raise `DexcomUnlinked` → persist

#### RefreshDexcomTokensCommand
- **Parameters**: `LinkId: Guid`
- **Flow**: Background scheduler → `RefreshDexcomTokensCommandHandler` → fetch new tokens → update DexcomLink aggregate → persist

### Event Context

#### AddFoodEventCommand
- **Parameters**: `UserId: Guid`, `EventTime: DateTimeOffset`, `Carbs: int`, `MealTagId: int`, `AbsorptionHint: enum`, `NoteText?: string`
- **Flow**: HTTP POST `/api/event/food` → handler → validate VO ranges → create Event aggregate + FoodDetails → raise `FoodEventCreated` → persist

#### AddInsulinEventCommand
- **Parameters**: `UserId: Guid`, `EventTime: DateTimeOffset`, `InsulinType: enum`, `Units: decimal`, `Preparation?: string`, `Delivery?: string`, `Timing?: string`, `NoteText?: string`
- **Flow**: HTTP POST `/api/event/insulin` → `AddInsulinEventCommandHandler` → validate → create Event aggregate + InsulinDetails → raise `InsulinEventCreated` → persist

#### AddExerciseEventCommand
- **Parameters**: `UserId: Guid`, `EventTime`, `TypeId: int`, `DurationMin: int`, `Intensity: enum`
- **Flow**: HTTP POST `/api/event/exercise` → validate → create → raise `ExerciseEventCreated` → persist

#### AddNoteEventCommand
- **Parameters**: `UserId: Guid`, `EventTime`, `Text: string`
- **Flow**: HTTP POST `/api/event/note` → validate length → create → raise `NoteEventCreated` → persist

## 6. Queries
- **GetUserPreferenceQuery**: GET `/api/user/preference` → returns user's TIRRange
  - Parameters: none
  - Flow: Actor(UI) → HTTP GET → `GetUserPreferenceQueryHandler` → load read model `UserPreferences` → return DTO

- **GetDexcomLinksQuery**: GET `/api/dexcom-link` → returns active dexcom links
  - Parameters: none
  - Flow: Actor(UI) → HTTP GET → `GetDexcomLinksQueryHandler` → query read model `DexcomLinkView` → return list of DTOs

- **ListEventsQuery**: GET `/api/event` → returns event history with optional filters
  - Parameters: `type?: EventType`, `from?: DateTime`, `to?: DateTime`
  - Flow: Actor(UI) → HTTP GET → `ListEventsQueryHandler` → apply filters on read model `EventHistory` → return array of event DTOs

- **GetEventDetailQuery**: GET `/api/event/{eventId}` → returns detailed event data
  - Parameters: `eventId: Guid`
  - Flow: Actor(UI) → HTTP GET → `GetEventDetailQueryHandler` → query read model `EventDetail` → return DTO

- **GetChartDataQuery**: GET `/api/chart` → returns glucose and event overlay data for chart
  - Parameters: `range: string` (e.g., "1h", "3h" etc.)
  - Flow: Actor(UI) → HTTP GET → `GetChartDataQueryHandler` → aggregate CGM readings and event overlays from projections → return time-series DTO

- **GetTimeInRangeQuery**: GET `/api/event/tir` → returns TIR percentage for window
  - Parameters: `from: DateTime`, `to: DateTime`
  - Flow: Actor(UI) → HTTP GET → `GetTimeInRangeQueryHandler` → compute TIR using read model `CGMData` and user preferences → return percentage DTO

## 7. Validation and business logic
- **RegisterUserCommand**: Email format, password length ≥12
- **UpdatePreferenceCommand**: `Lower < Upper`, both within [0, 1000]
- **LinkDexcomCommand**: `AuthorizationCode` non-empty
- **AddFoodEventCommand**: `0 ≤ Carbs ≤ 300`, MealTagId exists, AbsorptionHint valid, NoteText ≤500 chars
- **AddInsulinEventCommand**: `InsulinType` must be one of {Fast, Long}, `0 ≤ Units ≤ 100` and half-step, NoteText ≤500
- **AddExerciseEventCommand**: `1 ≤ DurationMin ≤ 300`, Intensity in enum
- **AddNoteEventCommand**: `1 ≤ Text.Length ≤ 500`
- **All Event Commands**: `EventTime ≤ now`, enforce immutability

## 8. API Endpoints

### Account
- POST `/api/user/register`  → RegisterUserCommand
- POST `/api/user/login`     → LoginUserCommand
- GET  `/api/user/preference`→ returns `TIRRange`
- PUT  `/api/user/preference`→ UpdatePreferenceCommand

### DexcomLink
- POST   `/api/dexcom-link`         → LinkDexcomCommand
- DELETE `/api/dexcom-link/{linkId}`→ UnlinkDexcomCommand

### Event
- GET   `/api/event`                     → List events (history) with filters (`type`, `from`, `to`)
- POST  `/api/event/food`                → AddFoodEventCommand
- POST  `/api/event/insulin`             → AddInsulinEventCommand
- POST  `/api/event/exercise`            → AddExerciseEventCommand
- POST  `/api/event/note`                → AddNoteEventCommand

*Assumptions:*
- User identity is resolved via JWT in each request.
- Read models for charting and TIR calculations are implemented separately as projections or query handlers outside of these write-side aggregates.
- Cron or scheduler for Dexcom polling exists but is configured outside of DDD contexts.
