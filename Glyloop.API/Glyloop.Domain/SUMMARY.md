# Domain Model Implementation Summary

## Overview
This document summarizes the initial implementation of the Domain layer for Glyloop, following Domain-Driven Design (DDD) principles and the architecture defined in `@ddd-plan.md` and `@backend.md`.

## Completed Steps (Iterations 1-4)

### Step 1: Domain Common Types ✅
Implemented foundational base classes and primitives that form the backbone of our domain model:

- **`Entity<TId>`**: Base class for entities with identity-based equality
  - Manages domain events collection
  - Provides `RaiseDomainEvent()` and `ClearDomainEvents()` methods
  
- **`ValueObject`**: Base class for value objects with structural equality
  - Immutable by design
  - Equality based on `GetEqualityComponents()`
  
- **`AggregateRoot<TId>`**: Base class for aggregate roots
  - Extends `Entity<TId>`
  - Entry point for repository access
  
- **`IDomainEvent` / `DomainEvent`**: Domain event infrastructure
  - Includes `EventId`, `OccurredAt`, `CorrelationId`, `CausationId`
  - Enables event-driven architecture
  
- **`Result<T>` / `Error`**: Railway-oriented programming pattern
  - Models expected domain failures without exceptions
  - Type-safe error handling
  
- **`ITimeProvider`**: Time abstraction for testability
  - Enables deterministic testing of time-dependent behavior

**Location**: `Glyloop.Domain/Common/`

### Step 2: Shared Value Objects ✅
Implemented immutable, validated value objects aligned with ubiquitous language:

#### Identity & Configuration
- **`UserId`**: Lightweight reference to authenticated user (ASP.NET Identity integration point)
- **`TirRange`**: Time-in-Range configuration (lower/upper glucose bounds in mg/dL)
  - Invariant: `Lower < Upper`, both 0-1000
  - Factory method: `Standard()` returns 70-180 mg/dL

#### Event-Specific Value Objects
- **`Carbohydrate`**: Food carbs in grams (0-300)
- **`InsulinDose`**: Insulin units (0-100, 0.5 increments)
- **`ExerciseDuration`**: Exercise time in minutes (1-300)
- **`NoteText`**: Note content (1-500 characters)
- **`MealTagId`**: Reference to meal categories (Breakfast, Lunch, etc.)
- **`ExerciseTypeId`**: Reference to exercise types (Walking, Running, etc.)

#### Enumerations
- **`AbsorptionHint`**: Food absorption rate (Rapid, Normal, Slow, Other)
- **`InsulinType`**: Fast-acting (bolus) or Long-acting (basal)
- **`IntensityType`**: Exercise intensity (Light, Moderate, Vigorous)
- **`EventType`**: Event categories (Food, Insulin, Exercise, Note)
- **`SourceType`**: Event origin (Manual, Imported, System)

All value objects:
- Validate invariants at construction
- Return `Result<T>` for expected validation failures
- Are immutable (readonly properties)
- Implement structural equality via `ValueObject` base

**Location**: `Glyloop.Domain/ValueObjects/`, `Glyloop.Domain/Enums/`

### Step 3: DexcomLink Aggregate ✅
Implemented the complete DexcomLink bounded context as an aggregate root:

#### Aggregate Structure
- **`DexcomLink`**: Aggregate root managing OAuth token lifecycle
  - **Identity**: `Guid`
  - **Properties**:
    - `UserId`: Owner of the link
    - `EncryptedAccessToken` / `EncryptedRefreshToken`: Encrypted OAuth tokens (byte arrays)
    - `TokenExpiresAt`: Token expiration timestamp
    - `LastRefreshedAt`: Last refresh timestamp
  - **Computed Properties**:
    - `IsActive`: Whether token is not expired
    - `ShouldRefresh`: Whether token should be proactively refreshed (within 1 hour)

#### Behaviors (Rich Domain Model)
- **`Create()`**: Factory method for creating new links after OAuth authorization
  - Validates token data and expiration
  - Raises `DexcomLinkedEvent`
  - Returns `Result<DexcomLink>` for validation failures
  
- **`RefreshTokens()`**: Updates tokens using OAuth refresh flow
  - Implements token rotation (both access and refresh tokens updated)
  - Validates new expiration time
  - Raises `DexcomTokensRefreshedEvent`
  - Returns `Result` for failures
  
- **`Unlink()`**: Marks link for removal
  - Raises `DexcomUnlinkedEvent` with purge flag

#### Domain Events
- **`DexcomLinkedEvent`**: Raised when account is linked
- **`DexcomUnlinkedEvent`**: Raised when account is unlinked (includes `DataPurged` flag)
- **`DexcomTokensRefreshedEvent`**: Raised when tokens are refreshed

#### Repository Interface
- **`IDexcomLinkRepository`**: Contract for aggregate persistence
  - `GetByIdAsync()`: Retrieve by link ID
  - `GetByUserIdAsync()`: Retrieve all links for a user
  - `GetActiveByUserIdAsync()`: Retrieve single active link
  - `GetLinksNeedingRefreshAsync()`: Find links needing proactive refresh (for background job)
  - `Add()` / `Remove()`: Lifecycle management

**Location**: `Glyloop.Domain/Aggregates/DexcomLink/`

### Step 4: Event Aggregate ✅
Implemented the complete Event bounded context with all event subtypes:

#### Aggregate Structure
- **`Event`**: Abstract aggregate root for all user events
  - **Identity**: `Guid`
  - **Base Properties**:
    - `UserId`: Owner of the event
    - `EventTime`: When the event occurred (DateTimeOffset with UTC offset)
    - `EventType`: Discriminator (Food, Insulin, Exercise, Note)
    - `Source`: Event origin (Manual, Imported, System)
    - `Note`: Optional note text (nullable)
    - `CreatedAt`: System creation timestamp
  - **Invariants**:
    - `EventTime ≤ now` (no future events)
    - Events are immutable after creation

#### Event Subtypes (Sealed Classes)
- **`FoodEvent`**: Food intake with carbohydrates
  - Properties: `Carbohydrates`, `MealTag`, `AbsorptionHint`
  - Validation: Carbs 0-300g via `Carbohydrate` VO
  
- **`InsulinEvent`**: Insulin administration
  - Properties: `InsulinType`, `Dose`, `Preparation?`, `Delivery?`, `Timing?`
  - Validation: Dose 0-100 units in 0.5 increments via `InsulinDose` VO
  
- **`ExerciseEvent`**: Physical activity session
  - Properties: `ExerciseType`, `Duration`, `Intensity`
  - Validation: Duration 1-300 minutes via `ExerciseDuration` VO
  
- **`NoteEvent`**: Standalone note or observation
  - Properties: `Text` (primary data for this type)
  - Validation: Text 1-500 chars via `NoteText` VO

#### Behaviors (Rich Domain Model)
- **`Create()`** (static factory for each subtype): 
  - Validates event time (not in future)
  - Creates immutable event instance
  - Raises appropriate domain event
  - Returns `Result<TEvent>` for validation failures

#### Domain Events
- **`FoodEventCreatedEvent`**: Raised when food event is created
- **`InsulinEventCreatedEvent`**: Raised when insulin event is created
- **`ExerciseEventCreatedEvent`**: Raised when exercise event is created
- **`NoteEventCreatedEvent`**: Raised when note event is created

All events include flattened data (primitives) for easy serialization and event sourcing.

#### Repository Interface
- **`IEventRepository`**: Contract for event persistence
  - `GetByIdAsync(eventId)`: Retrieve single event
  - `GetByUserIdAsync(userId, filters)`: Retrieve events with optional `EventType`, `from`, `to` filters
  - `GetPagedAsync(userId, from, to, page, size)`: Paginated retrieval for large histories
  - `CountByUserIdAsync(userId, from, to, eventType?)`: Count for pagination
  - `Add(event)`: Add new event (no Update - immutability)
  - `Remove(event)`: Soft or hard delete

**Location**: `Glyloop.Domain/Aggregates/Event/`

**Design Note**: Event subtypes use Table-Per-Type (TPT) pattern for persistence. Each subtype gets its own table with a discriminator in the base Event table. The `EventType` enum serves as the discriminator column.

### Domain Errors ✅
Centralized error catalog in `DomainErrors` static class:
- `User.*`: Email, TIR range validation errors
- `DexcomLink.*`: Token, authorization code errors
- `Event.*`: Event validation errors (carbs, insulin, duration, note, immutability)

**Location**: `Glyloop.Domain/Errors/`

## Design Principles Applied

### ✅ Domain Purity
- No infrastructure concerns (EF, HTTP, logging) in Domain layer
- Only business logic and invariants
- Persistence-agnostic (no data annotations)

### ✅ Invariant Enforcement
- All invariants validated at construction or mutation
- Guard clauses for primitive checks
- `Result<T>` pattern for expected failures
- Immutability for Value Objects

### ✅ Rich Domain Model (Not Anemic)
- Behaviors encapsulated in aggregates (`Create()`, `RefreshTokens()`, `Unlink()`)
- Business rules co-located with data
- Expressive method names aligned with ubiquitous language

### ✅ Domain Events
- Events raised at meaningful transitions
- Include correlation/causation IDs for traceability
- Enable event-driven architecture and eventual consistency

### ✅ Testability
- `ITimeProvider` abstraction for deterministic time-based tests
- Factory methods for complex creation paths
- Clear separation of concerns

## Mapping Hints for Infrastructure Layer

### DexcomLink Aggregate
- **`EncryptedAccessToken`**, **`EncryptedRefreshToken`**: Store as `VARBINARY(MAX)` or equivalent
- **`TokenExpiresAt`**, **`LastRefreshedAt`**: Store as `DATETIMEOFFSET` to preserve UTC offset
- **Index**: Add index on `UserId` for efficient user-based queries
- **Constraint**: Consider unique constraint on `UserId` if only one active link per user

### Value Objects
- **`TirRange`**: Can be stored as owned entity type (complex type) or separate columns
- **`UserId`**: Maps to `Guid` column (FK to ASP.NET Identity Users table)
- **Enums**: Store as `INT` with name mapping or as string for readability

## Alignment with DDD Plan

| DDD Plan Section | Implementation Status | Reference |
|-----------------|----------------------|-----------|
| **Section 2 - Aggregates (DexcomLink)** | ✅ Complete | `Aggregates/DexcomLink/` |
| **Section 2 - Aggregates (Event)** | ✅ Complete | `Aggregates/Event/` |
| **Section 3 - Value Objects** | ✅ Complete | `ValueObjects/`, `Enums/` |
| **Section 4 - Domain Events (DexcomLink)** | ✅ Complete | `Aggregates/DexcomLink/Events/` |
| **Section 4 - Domain Events (Event)** | ✅ Complete | `Aggregates/Event/Events/` |

## Files Created

```
Glyloop.Domain/
├── Common/
│   ├── Entity.cs
│   ├── ValueObject.cs
│   ├── AggregateRoot.cs
│   ├── IDomainEvent.cs
│   ├── DomainEvent.cs
│   ├── Result.cs
│   ├── Error.cs
│   └── ITimeProvider.cs
├── Errors/
│   └── DomainErrors.cs
├── ValueObjects/
│   ├── UserId.cs
│   ├── TirRange.cs
│   ├── Carbohydrate.cs
│   ├── InsulinDose.cs
│   ├── ExerciseDuration.cs
│   ├── NoteText.cs
│   ├── MealTagId.cs
│   └── ExerciseTypeId.cs
├── Enums/
│   ├── AbsorptionHint.cs
│   ├── InsulinType.cs
│   ├── IntensityType.cs
│   ├── EventType.cs
│   └── SourceType.cs
├── Aggregates/
│   ├── DexcomLink/
│   │   ├── DexcomLink.cs
│   │   └── Events/
│   │       ├── DexcomLinkedEvent.cs
│   │       ├── DexcomUnlinkedEvent.cs
│   │       └── DexcomTokensRefreshedEvent.cs
│   └── Event/
│       ├── Event.cs (abstract base)
│       ├── FoodEvent.cs
│       ├── InsulinEvent.cs
│       ├── ExerciseEvent.cs
│       ├── NoteEvent.cs
│       └── Events/
│           ├── FoodEventCreatedEvent.cs
│           ├── InsulinEventCreatedEvent.cs
│           ├── ExerciseEventCreatedEvent.cs
│           └── NoteEventCreatedEvent.cs
└── Repositories/
    ├── IDexcomLinkRepository.cs
    └── IEventRepository.cs
```

## Validation Strategy

### Construction-Time Validation
- Value Objects validate all invariants in `Create()` factory methods
- Return `Result<T>` for expected failures
- Throw `ArgumentException` only for programming errors (null, empty GUIDs)

### Mutation-Time Validation
- Aggregate behaviors (e.g., `RefreshTokens()`) validate inputs
- Return `Result` to signal domain rule violations
- Enforce immutability rules (Events cannot be modified after creation)

## Notes & Trade-offs

1. **ASP.NET Identity Integration**: User is NOT modeled as a domain aggregate. Instead, `UserId` serves as a lightweight reference. This keeps the domain decoupled from identity infrastructure.

2. **Token Encryption**: Encryption is handled in the Infrastructure layer. Domain stores `byte[]` but doesn't know encryption implementation.

3. **Time Abstraction**: `ITimeProvider` enables testability but adds slight complexity. Worth it for deterministic tests.

4. **MealTagId / ExerciseTypeId**: Modeled as simple value objects. The actual tag/type definitions could live in a separate bounded context or reference data table.

5. **Result<T> Pattern**: Chosen over exceptions for expected domain failures. Makes success/failure paths explicit and reduces exception handling noise.

6. **Persistence Strategy**: Table-Per-Type (TPT) for Event aggregate subtypes. Each subtype gets its own table for better data organization and query flexibility.

7. **Token Refresh (MVP)**: Manual refresh via API endpoint only. No background worker for MVP to keep initial complexity low. The domain model supports both approaches.

## Testing Considerations

### Unit Test Coverage Needed
- ✅ Value Object creation with valid/invalid inputs
- ✅ TirRange boundary cases (0, 1000, lower >= upper)
- ✅ Carbohydrate, InsulinDose validation (ranges, increments)
- ✅ DexcomLink creation with expired tokens
- ✅ DexcomLink token refresh with rotation
- ✅ Event creation with future EventTime (should fail)
- ✅ Event immutability (no modification after creation)
- ✅ FoodEvent, InsulinEvent, ExerciseEvent, NoteEvent creation
- ✅ Domain events raised at correct transitions
- ✅ Entity equality (identity-based)
- ✅ Value Object equality (structural)

### Property-Based Test Opportunities
- TirRange with random valid/invalid values
- InsulinDose 0.5 increment validation
- NoteText length constraints

---

---

**Implementation Date**: October 19, 2025  
**Last Updated**: October 19, 2025 (Added Event Aggregate - Step 4, Refactored to plain Guid IDs, Moved repositories to centralized folder, Confirmed architectural decisions)  
**DDD Plan Reference**: `.ai/ddd-plan.md`  
**Architecture Rules**: `.cursor/rules/backend.md`, `.cursor/rules/architecture.md`

