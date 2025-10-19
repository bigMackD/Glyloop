# Next Steps: Domain Model Implementation

## Completed ✅
1. **Domain Common Types** - Base classes for Entity, ValueObject, AggregateRoot, DomainEvent, Result<T>, Error
2. **Shared Value Objects** - UserId, TirRange, event-specific VOs, and enumerations
3. **DexcomLink Aggregate** - Complete implementation with behaviors, domain events, and repository interface
4. **Event Aggregate** - Complete implementation with all four subtypes (Food, Insulin, Exercise, Note), domain events, and repository interface

---

## Next 3 Priority Actions

### 🔴 Step 5: Domain Unit Tests (HIGH PRIORITY)
**Scope**: Comprehensive unit tests for all domain models, value objects, aggregates, and behaviors.

**Deliverables**:
- **Value Object Tests**:
  - `TirRange`: valid ranges, invalid ranges (lower >= upper, out of bounds)
  - `Carbohydrate`: valid grams (0-300), invalid values
  - `InsulinDose`: valid doses with 0.5 increments, invalid values
  - `ExerciseDuration`: valid minutes (1-300), invalid values
  - `NoteText`: valid text (1-500 chars), invalid (empty, too long)
  - `UserId`: creation, empty GUID validation
  - Structural equality tests for all VOs

- **DexcomLink Aggregate Tests**:
  - `Create()`: valid creation, expired token validation
  - `RefreshTokens()`: successful refresh, token rotation, expiration validation
  - `Unlink()`: raises correct event
  - Domain event raising verification

- **Event Aggregate Tests**:
  - Base `Event`: future EventTime validation (should fail)
  - `FoodEvent.Create()`: valid creation, domain event raised
  - `InsulinEvent.Create()`: valid creation with optional fields
  - `ExerciseEvent.Create()`: valid creation
  - `NoteEvent.Create()`: valid creation
  - Event immutability (no modification methods exist)

- **Domain Event Tests**:
  - Event metadata (EventId, OccurredAt, CorrelationId, CausationId)
  - Serialization/deserialization (if needed)

- **Result<T> Pattern Tests**:
  - Success path, failure path
  - Value access throws on failure
  - Error handling

**Testing Framework**: xUnit (already in `Glyloop.Domain.Tests.csproj`)

**Files to Create**:
```
Glyloop.Domain.Tests/
├── ValueObjects/
│   ├── TirRangeTests.cs
│   ├── CarbohydrateTests.cs
│   ├── InsulinDoseTests.cs
│   ├── ExerciseDurationTests.cs
│   ├── NoteTextTests.cs
│   └── UserIdTests.cs
├── Aggregates/
│   ├── DexcomLink/
│   │   └── DexcomLinkTests.cs
│   └── Event/
│       ├── FoodEventTests.cs
│       ├── InsulinEventTests.cs
│       ├── ExerciseEventTests.cs
│       └── NoteEventTests.cs
└── Common/
    └── ResultTests.cs
```

**Reference**: DDD Plan Section 7 (Validation), Testing best practices

---

### 🟡 Step 6: Implement Specifications for Complex Business Rules (MEDIUM PRIORITY)
**Scope**: Create reusable, composable business rule specifications for query filtering and validation.

**Deliverables**:
- Base `Specification<T>` class with:
  - `IsSatisfiedBy(T entity)` method
  - Combinators: `And()`, `Or()`, `Not()`
  - `ToExpression()` for EF Core query integration (if needed)
- Concrete specifications:
  - `EventInDateRangeSpecification` - filters events by start/end dates
  - `EventByTypeSpecification` - filters events by EventType
  - `DexcomLinkNeedsRefreshSpecification` - identifies links expiring within threshold
  - `TirRangeValidSpecification` - validates TIR range business rules (if not already in VO)
  - `EventIsEditableSpecification` - checks if an event can be modified (for future extensions)

**Reference**: DDD Plan Section 2 (Aggregates - specs), Architecture best practices

**Design Considerations**:
- Keep specifications focused and single-purpose
- Make them composable via combinators
- Can be used in-memory or translated to EF Core expressions
- Useful for both validation and querying

**Files to Create**:
```
Glyloop.Domain/Specifications/
├── Specification.cs (base class)
├── EventInDateRangeSpecification.cs
├── EventByTypeSpecification.cs
├── DexcomLinkNeedsRefreshSpecification.cs
└── CompositeSpecification.cs (And/Or/Not helpers)
```

---

### 🟢 Step 7: Domain Services (if needed) and Policies (LOW PRIORITY)
**Scope**: Identify and implement domain services for behavior that doesn't naturally fit in an aggregate.

**Potential Domain Services**:
- **`TirCalculationService`**: 
  - Computes Time-in-Range percentage for a given time window
  - Takes `IEnumerable<GlucoseReading>`, `TirRange`, `from`, `to`
  - Returns percentage in range (pure domain logic, no infrastructure)
  - Could be a static utility or an injectable service if it needs configuration

- **`EventOverlapPolicy`**:
  - Business policy: should we warn users if two food events overlap within X minutes?
  - Returns validation messages/warnings without preventing creation

**Design Considerations**:
- Only create services if behavior truly doesn't belong to an entity/VO
- Keep services stateless and focused on domain logic
- Avoid creating services for simple CRUD or query operations (those belong in Application layer)

**Defer if Not Needed**: If all logic fits cleanly in aggregates/VOs, skip this step.

**Files to Create (if needed)**:
```
Glyloop.Domain/Services/
├── TirCalculationService.cs
└── Policies/
    └── EventOverlapPolicy.cs
```

---

## Future Steps (Beyond Next 3)

### Step 8: Repository Implementations (Infrastructure Layer)
- Move to Infrastructure layer to implement `IDexcomLinkRepository` and `IEventRepository`
- Configure EF Core mappings (IEntityTypeConfiguration<T>)
- Set up migrations
- Implement Unit of Work pattern

### Step 9: Application Layer - Commands & Handlers
- Implement commands from DDD Plan Section 5:
  - `LinkDexcomCommand`, `UnlinkDexcomCommand`, `RefreshDexcomTokensCommand`
  - `AddFoodEventCommand`, `AddInsulinEventCommand`, `AddExerciseEventCommand`, `AddNoteEventCommand`
- MediatR handlers that orchestrate domain aggregates
- Validation pipeline with FluentValidation

### Step 10: Application Layer - Queries & Handlers
- Implement queries from DDD Plan Section 6:
  - `GetDexcomLinksQuery`, `ListEventsQuery`, `GetEventDetailQuery`
  - `GetChartDataQuery`, `GetTimeInRangeQuery`
- Read model projections for query optimization
- DTOs for API responses

---

## Architectural Decisions (Confirmed)

1. **Event Aggregate Persistence Strategy**: ✅ **Table-Per-Type (TPT) with discriminator in EF Core**
   - Each event subtype (FoodEvent, InsulinEvent, ExerciseEvent, NoteEvent) gets its own table
   - Base Event table contains common properties (Id, UserId, EventTime, EventType, Source, Note, CreatedAt)
   - Subtype tables contain specific properties (e.g., FoodEvents table has Carbohydrates, MealTagId, AbsorptionHint)
   - Discriminator column in base table for polymorphic queries

2. **Glucose Readings**: ✅ **Deferred - Read-only data imported from Dexcom**
   - Not modeled as domain aggregates in MVP
   - Will be defined when Application layer queries are implemented
   - Likely stored as read-only projections/views for charting and TIR calculations

3. **User Preferences**: ✅ **TirRange remains a Value Object**
   - Current approach is sufficient for MVP
   - If preferences expand (e.g., notification settings, display units), consider creating a lightweight `UserPreference` aggregate

4. **Token Encryption**: ✅ **ITokenEncryptionService in Infrastructure layer**
   - Domain stores `byte[]` (persistence-agnostic)
   - Infrastructure provides `ITokenEncryptionService` implementation
   - Application command handlers inject the service to encrypt/decrypt before passing to domain

5. **Token Refresh for MVP**: ✅ **No background worker**
   - Manual refresh via API endpoint for MVP
   - Future enhancement: Background worker (Hangfire/Quartz) for automatic refresh
   - Domain `DexcomLink.RefreshTokens()` method ready for both manual and automated scenarios

---

## Progress Tracker

| Step | Status | Completion % |
|------|--------|--------------|
| 1. Domain Common Types | ✅ Complete | 100% |
| 2. Shared Value Objects | ✅ Complete | 100% |
| 3. DexcomLink Aggregate | ✅ Complete | 100% |
| 4. Event Aggregate | ✅ Complete | 100% |
| 5. Domain Unit Tests | ⏳ Pending | 0% |
| 6. Specifications | ⏳ Pending | 0% |
| 7. Domain Services | ⏳ Pending | 0% |
| 8. Infrastructure | ⏳ Pending | 0% |
| 9. Application Commands | ⏳ Pending | 0% |
| 10. Application Queries | ⏳ Pending | 0% |

**Overall Domain Layer Completion**: ~60% (4 of 7 core domain modeling steps complete)

---

**Next Review Point**: After completing Domain Unit Tests (Step 5)  
**Blocked By**: None - ready to proceed  
**Dependencies**: None - domain is independent of other layers

