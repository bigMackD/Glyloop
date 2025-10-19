# Glyloop Domain Layer

## Overview
This is the **Domain Layer** of the Glyloop application, implementing Domain-Driven Design (DDD) principles to model the core business logic for diabetes management. The domain is persistence-agnostic, focusing purely on business rules, invariants, and behaviors.

## Purpose
The Domain layer:
- Encapsulates business logic and invariants
- Defines aggregates, entities, value objects, and domain events
- Enforces consistency boundaries through aggregate roots
- Remains independent of infrastructure concerns (databases, HTTP, external services)
- Uses ubiquitous language aligned with domain experts

## Architecture Principles

### 🎯 Domain Purity
- **No infrastructure dependencies**: No EF Core attributes, no HTTP concerns, no logging frameworks
- **No data annotations**: Persistence mapping is handled in Infrastructure layer via `IEntityTypeConfiguration<T>`
- **Business logic only**: Pure C# models focused on behavior and invariants

### 🏗️ DDD Building Blocks
- **Aggregates**: Consistency boundaries with transactional integrity (`DexcomLink`, `Event`)
- **Entities**: Objects with identity (`Entity<TId>`)
- **Value Objects**: Immutable objects defined by attributes (`TirRange`, `Carbohydrate`, etc.)
- **Domain Events**: Signals of meaningful state changes (`DexcomLinkedEvent`, `FoodEventCreated`, etc.)
- **Repositories** (interfaces only): Contracts for aggregate persistence
- **Specifications**: Composable business rule predicates (future)

### 🛡️ Invariant Enforcement
- Invariants validated at construction (`Create()` factory methods)
- Mutations return `Result<T>` for expected failures
- Immutability enforced for Value Objects
- Guard clauses for primitive checks

### 🎭 Rich Domain Model
- Behavior co-located with data (not anemic)
- Expressive method names aligned with ubiquitous language
- Aggregates expose methods like `RefreshTokens()`, `Unlink()`, not just property setters

## Structure

```
Glyloop.Domain/
├── Common/                     # Base types for all domain models
│   ├── Entity.cs               # Base entity with identity equality
│   ├── ValueObject.cs          # Base value object with structural equality
│   ├── AggregateRoot.cs        # Base aggregate root
│   ├── IDomainEvent.cs         # Domain event interface
│   ├── DomainEvent.cs          # Base domain event with metadata
│   ├── Result.cs               # Result<T> for railway-oriented programming
│   ├── Error.cs                # Domain error type
│   └── ITimeProvider.cs        # Time abstraction for testability
│
├── Aggregates/                 # Aggregate roots and their child entities
│   ├── DexcomLink/             # OAuth token lifecycle management
│   │   ├── DexcomLink.cs       # Aggregate root (Id: Guid)
│   │   └── Events/
│   │       ├── DexcomLinkedEvent.cs
│   │       ├── DexcomUnlinkedEvent.cs
│   │       └── DexcomTokensRefreshedEvent.cs
│   │
│   └── Event/                  # User events (Food, Insulin, Exercise, Note)
│       ├── Event.cs            # Abstract aggregate root (Id: Guid)
│       ├── FoodEvent.cs        # Food intake events
│       ├── InsulinEvent.cs     # Insulin administration events
│       ├── ExerciseEvent.cs    # Exercise events
│       ├── NoteEvent.cs        # Standalone note events
│       └── Events/
│           ├── FoodEventCreatedEvent.cs
│           ├── InsulinEventCreatedEvent.cs
│           ├── ExerciseEventCreatedEvent.cs
│           └── NoteEventCreatedEvent.cs
│
│
├── ValueObjects/               # Immutable, validated value objects
│   ├── UserId.cs               # User identity reference
│   ├── TirRange.cs             # Time-in-Range glucose bounds
│   ├── Carbohydrate.cs         # Food carbs (0-300g)
│   ├── InsulinDose.cs          # Insulin units (0-100, 0.5 increments)
│   ├── ExerciseDuration.cs     # Exercise time (1-300 min)
│   ├── NoteText.cs             # Note content (1-500 chars)
│   ├── MealTagId.cs            # Meal category reference
│   └── ExerciseTypeId.cs       # Exercise type reference
│
├── Enums/                      # Domain enumerations
│   ├── AbsorptionHint.cs       # Food absorption rate
│   ├── InsulinType.cs          # Fast vs Long insulin
│   ├── IntensityType.cs        # Exercise intensity
│   ├── EventType.cs            # Event categories
│   └── SourceType.cs           # Event origin (Manual, Imported, System)
│
├── Errors/                     # Centralized domain error catalog
│   └── DomainErrors.cs         # Static error definitions
│
├── Repositories/               # Repository interfaces (contracts only)
│   ├── IDexcomLinkRepository.cs
│   └── IEventRepository.cs
│
├── Services/                   # Domain services (future)
│   └── (TirCalculationService, etc.)
│
├── Specifications/             # Reusable business rule predicates (future)
│   └── (Specification<T> base class)
│
├── README.md                   # This file
├── SUMMARY.md                  # Implementation summary
└── NEXT-STEPS.md               # Next 3 priority actions
```

## Bounded Contexts

### 1. DexcomLink Context ✅
**Purpose**: Manages linking/unlinking Dexcom Sandbox accounts and OAuth token lifecycle.

**Aggregate Root**: `DexcomLink`
- **Properties**: `UserId`, `EncryptedAccessToken`, `EncryptedRefreshToken`, `TokenExpiresAt`, `LastRefreshedAt`
- **Invariants**: 
  - `TokenExpiresAt > now` for active links
  - Tokens are encrypted (encryption handled in Infrastructure)
  - Refresh operations follow OAuth token rotation policy
- **Behaviors**: `Create()`, `RefreshTokens()`, `Unlink()`
- **Domain Events**: `DexcomLinkedEvent`, `DexcomUnlinkedEvent`, `DexcomTokensRefreshedEvent`

**Repository**: `IDexcomLinkRepository` (in `Repositories/`)
- `GetByIdAsync(Guid linkId)`, `GetActiveByUserIdAsync(userId)`, `GetLinksNeedingRefreshAsync()` (for manual refresh in MVP)

### 2. Event Context ✅
**Purpose**: Manages immutable user events (Food, Insulin, Exercise, Note) for diabetes management.

**Aggregate Root**: `Event` (abstract base)
- **Subtypes**: `FoodEvent`, `InsulinEvent`, `ExerciseEvent`, `NoteEvent`
- **Properties**: `Id` (Guid), `UserId`, `EventTime`, `EventType`, `Source`, `Note` (optional), `CreatedAt`
- **Invariants**:
  - `EventTime ≤ now` (no future events)
  - Events are immutable after creation
  - Subtype-specific validations (carbs 0-300, insulin dose 0-100, etc.)
- **Behaviors**: `Create()` factory methods (one per subtype) with validation
- **Domain Events**: `FoodEventCreatedEvent`, `InsulinEventCreatedEvent`, `ExerciseEventCreatedEvent`, `NoteEventCreatedEvent`

**Repository**: `IEventRepository` (in `Repositories/`)
- `GetByIdAsync(Guid eventId)`, `GetByUserIdAsync(userId, filters)`, `GetPagedAsync(...)`, `Add(event)`

### 3. Account Context 🔗 (External - ASP.NET Identity)
**Purpose**: User registration, authentication, and basic preferences.

**Integration Point**: `UserId` value object
- The `User` entity is managed by ASP.NET Core Identity (`IdentityUser`)
- Domain references users via `UserId` (Guid) to remain infrastructure-independent
- User preferences (TIR range) may be managed via a lightweight `UserPreference` aggregate in the future

## Key Design Patterns

### 1. Result<T> Pattern (Railway-Oriented Programming)
Expected domain failures return `Result<T>` instead of throwing exceptions:

```csharp
var tirRangeResult = TirRange.Create(70, 180);
if (tirRangeResult.IsFailure)
{
    return tirRangeResult.Error; // DomainErrors.User.InvalidTirRange
}
var tirRange = tirRangeResult.Value;
```

### 2. Factory Methods with Validation
Aggregates and Value Objects use static `Create()` methods:

```csharp
var link = DexcomLink.Create(userId, token, refreshToken, expiresAt, timeProvider, corrId, causeId);
if (link.IsSuccess)
{
    repository.Add(link.Value);
}
```

### 3. Domain Events
Aggregates raise events at meaningful transitions:

```csharp
// Inside DexcomLink.Create()
RaiseDomainEvent(new DexcomLinkedEvent(userId, linkId, correlationId, causationId));

// Infrastructure dispatches events after persistence
foreach (var domainEvent in aggregate.DomainEvents)
{
    await _mediator.Publish(domainEvent);
}
aggregate.ClearDomainEvents();
```

### 4. Value Object Equality
Value Objects compare by structure, not identity:

```csharp
var range1 = TirRange.Create(70, 180).Value;
var range2 = TirRange.Create(70, 180).Value;
Assert.True(range1 == range2); // Structural equality
```

### 5. Entity Equality
Entities compare by ID only:

```csharp
var link1 = DexcomLink.Create(...).Value;
var link2 = repository.GetById(link1.Id);
Assert.True(link1 == link2); // Same ID = same entity
```

## Validation Strategy

### Construction-Time Validation
- Value Objects validate in `Create()` factory methods
- Return `Result<T>` for expected failures
- Throw `ArgumentException` for programming errors (e.g., null)

```csharp
public static Result<Carbohydrate> Create(int grams)
{
    if (grams < 0 || grams > 300)
        return Result.Failure<Carbohydrate>(DomainErrors.Event.InvalidCarbohydrates);
    
    return Result.Success(new Carbohydrate(grams));
}
```

### Mutation-Time Validation
- Aggregate behaviors validate inputs before state changes
- Return `Result` to signal failures

```csharp
public Result RefreshTokens(byte[] newToken, ...)
{
    if (newToken == null || newToken.Length == 0)
        return Result.Failure(Error.Create("DexcomLink.InvalidToken", "Token cannot be empty"));
    
    // Update state
    EncryptedAccessToken = newToken;
    RaiseDomainEvent(new DexcomTokensRefreshedEvent(...));
    
    return Result.Success();
}
```

## Persistence Mapping Hints

The Domain layer is persistence-agnostic. Here are hints for Infrastructure layer mapping:

### DexcomLink
- **Table**: `DexcomLinks`
- **`EncryptedAccessToken`**, **`EncryptedRefreshToken`**: `VARBINARY(MAX)` or equivalent
- **`TokenExpiresAt`**, **`LastRefreshedAt`**: `DATETIMEOFFSET` (preserves UTC offset)
- **Index**: `UserId` for efficient user-based queries
- **Constraint**: Unique constraint on `UserId` if only one active link per user

### Value Objects
- **`TirRange`**: Owned entity type (complex type) or separate columns (`TirLower`, `TirUpper`)
- **`UserId`**: `Guid` FK to `AspNetUsers.Id`
- **Enums**: Store as `INT` with mapping or `NVARCHAR` for readability

### Event Aggregate
- **Strategy**: Table-Per-Type (TPT) with discriminator in EF Core
- **Base Table**: `Events` with common properties (Id, UserId, EventTime, EventType, Source, Note, CreatedAt)
- **Subtype Tables**: 
  - `FoodEvents` (Carbohydrates, MealTagId, AbsorptionHint)
  - `InsulinEvents` (InsulinType, Dose, Preparation, Delivery, Timing)
  - `ExerciseEvents` (ExerciseTypeId, DurationMinutes, Intensity)
  - `NoteEvents` (Text)
- **Discriminator**: `EventType` column in base table (INT or NVARCHAR)
- **`EventTime`**, **`CreatedAt`**: `DATETIMEOFFSET`
- **Indexes**: `UserId`, `EventTime`, `EventType` on base table for query performance

## Testing Considerations

### Unit Test Coverage
- ✅ Value Object creation with valid/invalid inputs
- ✅ Aggregate behaviors (Create, RefreshTokens, Unlink)
- ✅ Domain events raised at correct transitions
- ✅ Result<T> success/failure paths
- ✅ Entity equality (identity-based)
- ✅ Value Object equality (structural)
- ✅ Invariant enforcement

### Test Examples
```csharp
[Fact]
public void TirRange_Create_WithLowerGreaterThanUpper_ReturnsFailure()
{
    var result = TirRange.Create(180, 70);
    
    Assert.True(result.IsFailure);
    Assert.Equal(DomainErrors.User.InvalidTirRange, result.Error);
}

[Fact]
public void DexcomLink_RefreshTokens_RaisesTokensRefreshedEvent()
{
    var link = CreateValidLink();
    
    link.RefreshTokens(newToken, newRefreshToken, newExpiry, timeProvider, corrId, causeId);
    
    Assert.Single(link.DomainEvents);
    Assert.IsType<DexcomTokensRefreshedEvent>(link.DomainEvents.First());
}
```

## Dependencies

### Internal (Glyloop)
- None - Domain is the innermost layer

### External (NuGet)
- None - Domain has zero external dependencies for maximum purity

## Usage by Other Layers

### Application Layer
- Orchestrates aggregates via commands/queries
- Calls aggregate factory methods and behaviors
- Publishes domain events via MediatR
- Maps domain entities to DTOs

```csharp
// In LinkDexcomCommandHandler
var linkResult = DexcomLink.Create(userId, encryptedToken, ...);
if (linkResult.IsSuccess)
{
    _repository.Add(linkResult.Value);
    await _unitOfWork.CommitAsync(); // Infrastructure
}
return linkResult.IsSuccess 
    ? Result.Success() 
    : Result.Failure(linkResult.Error);
```

### Infrastructure Layer
- Implements repository interfaces
- Configures EF Core mappings
- Handles encryption/decryption
- Dispatches domain events after persistence

```csharp
// In DexcomLinkRepository
public async Task<DexcomLink?> GetByIdAsync(DexcomLinkId linkId, CancellationToken ct)
{
    return await _context.DexcomLinks
        .FirstOrDefaultAsync(l => l.Id == linkId, ct);
}
```

### API Layer
- Does NOT reference Domain directly (goes through Application)
- Receives DTOs from Application layer
- Maps HTTP requests to commands/queries

## References

- **DDD Plan**: `.ai/ddd-plan.md`
- **Backend Architecture Rules**: `.cursor/rules/backend.md`
- **Architecture Principles**: `.cursor/rules/architecture.md`
- **Implementation Summary**: `SUMMARY.md`
- **Next Steps**: `NEXT-STEPS.md`

## Contributing

When extending the Domain layer:
1. Follow DDD principles (aggregates, entities, value objects)
2. Enforce invariants at construction and mutation
3. Use `Result<T>` for expected failures
4. Raise domain events at meaningful transitions
5. Keep the layer persistence-agnostic (no EF attributes)
6. Write unit tests for all behaviors and invariants
7. Update `SUMMARY.md` and `NEXT-STEPS.md` after changes

---

**Last Updated**: October 19, 2025  
**Status**: 🟢 Core Complete (Both DexcomLink and Event contexts implemented)  
**Maintainer**: Glyloop Development Team

