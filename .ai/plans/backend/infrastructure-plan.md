# Glyloop Infrastructure Layer - Implementation Plan

## Overview
This document outlines the implementation plan for the Infrastructure layer of Glyloop, which bridges the domain model to PostgreSQL persistence, ASP.NET Core Identity, Dexcom API integration, and cross-cutting concerns like encryption and time management.

## Context
- **Domain Layer**: ✅ Complete (DexcomLink and Event aggregates with all value objects)
- **Database**: PostgreSQL with connection pooling, JSONB for flexibility
- **Framework**: .NET 8, EF Core, ASP.NET Core Identity
- **External APIs**: Dexcom Sandbox (OAuth + CGM polling)
- **Security**: Token encryption at rest, httpOnly cookies, CSRF protection

## Implementation Phases

### Phase 1: Persistence Foundation (Priority: HIGH)

#### 1.1 DbContext Setup
**Files**: `Persistence/GlyloopDbContext.cs`

- Configure `DbContext` for PostgreSQL (Npgsql)
- Add `DbSet<DexcomLink>` and `DbSet<Event>` (polymorphic)
- Integrate ASP.NET Core Identity (`IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>`)
- Configure model scanning for entity configurations
- Override `SaveChangesAsync` for domain event interception

**Key Decisions**:
- Use `DateTimeOffset` (maps to PostgreSQL `TIMESTAMPTZ`) for all timestamps
- Store in UTC, preserve timezone offset for display
- Use `Guid` for all Identity keys (ApplicationUser, roles, etc.)
- Connection string from `appsettings.json` with environment-specific overrides

#### 1.1.5 ApplicationUser Entity
**File**: `Identity/ApplicationUser.cs`

**Purpose**: Custom user entity extending ASP.NET Core Identity

**Structure**:
```csharp
public class ApplicationUser : IdentityUser<Guid>
{
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
    
    // Navigation properties (for Infrastructure queries only, not exposed to Domain)
    public ICollection<DexcomLink> DexcomLinks { get; set; } = new List<DexcomLink>();
    public ICollection<Event> Events { get; set; } = new List<Event>();
}
```

**Entity Configuration**:
**File**: `Persistence/Configurations/ApplicationUserConfiguration.cs`

```csharp
public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(u => u.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("NOW()");
        
        builder.Property(u => u.LastLoginAt)
            .HasColumnType("timestamptz");
        
        builder.HasMany(u => u.DexcomLinks)
            .WithOne()
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasMany(u => u.Events)
            .WithOne()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

**Design Notes**:
- Uses `Guid` as primary key (not default `string`)
- Domain uses `UserId` value object; Infrastructure uses `ApplicationUser`
- No Identity types in Domain layer (maintains separation)

#### 1.2 Entity Type Configurations
**Files**: `Persistence/Configurations/`

**DexcomLinkConfiguration.cs**:
```csharp
- Table: "DexcomLinks"
- Primary key: Id (Guid)
- UserId: Guid, indexed, FK to AspNetUsers.Id (ApplicationUser)
- EncryptedAccessToken: byte[] → BYTEA
- EncryptedRefreshToken: byte[] → BYTEA  
- TokenExpiresAt: DateTimeOffset → TIMESTAMPTZ
- LastRefreshedAt: DateTimeOffset → TIMESTAMPTZ
- Computed properties (IsActive, ShouldRefresh): ignore
- Index: IX_DexcomLinks_UserId
- Optional: Unique constraint on UserId (one link per user)
```

**EventConfiguration.cs** (Base):
```csharp
- Table: "Events"
- TPT (Table-Per-Type) inheritance strategy
- Primary key: Id (Guid)
- UserId: Guid, indexed, FK to AspNetUsers.Id (ApplicationUser)
- EventTime: DateTimeOffset → TIMESTAMPTZ
- CreatedAt: DateTimeOffset → TIMESTAMPTZ
- EventType: int (discriminator)
- Source: int
- Note: string (500), nullable
- Composite index: IX_Events_UserId_EventTime
- Index: IX_Events_EventType
```

**ApplicationUserConfiguration.cs**:
```csharp
- Table: "AspNetUsers" (managed by Identity)
- Primary key: Id (Guid)
- CreatedAt: DateTimeOffset → TIMESTAMPTZ with default NOW()
- LastLoginAt: DateTimeOffset → TIMESTAMPTZ, nullable
- Navigation properties: DexcomLinks, Events
- OnDelete: Cascade (when user deleted, remove all their data)
```

**FoodEventConfiguration.cs**:
```csharp
- Table: "FoodEvents"
- TPT child of Event
- Carbohydrates: value conversion (Carbohydrate → int Grams)
- MealTagId: Guid, nullable
- AbsorptionHint: int, nullable
```

**InsulinEventConfiguration.cs**:
```csharp
- Table: "InsulinEvents"
- TPT child of Event
- InsulinType: int (Fast=0, Long=1)
- Dose: value conversion (InsulinDose → decimal(5,2) Units)
- Preparation: string, nullable
- Delivery: string, nullable
- Timing: string, nullable
```

**ExerciseEventConfiguration.cs**:
```csharp
- Table: "ExerciseEvents"
- TPT child of Event
- ExerciseTypeId: Guid
- Duration: value conversion (ExerciseDuration → int Minutes)
- Intensity: int, nullable
```

**NoteEventConfiguration.cs**:
```csharp
- Table: "NoteEvents"
- TPT child of Event
- Text: value conversion (NoteText → string(500) Value)
```

**Value Converters**:
- Create reusable value converters for all VOs (Carbohydrate, InsulinDose, ExerciseDuration, NoteText, UserId)
- Pattern: `VO → primitive` for storage, `primitive → VO` for retrieval

#### 1.3 Initial Migration
**Command**: `dotnet ef migrations add InitialCreate`

**Verify**:
- All tables created (DexcomLinks, Events, FoodEvents, InsulinEvents, ExerciseEvents, NoteEvents, GlucoseReadings)
- Identity tables with Guid keys (AspNetUsers with CreatedAt/LastLoginAt, AspNetRoles, AspNetUserRoles, etc.)
- Correct column types (BYTEA, TIMESTAMPTZ, proper string lengths)
- All indexes present (including composite indexes on Events)
- Foreign key constraints configured (UserId → AspNetUsers.Id with CASCADE)
- Discriminator column on Events table (EventType)

**Test**: Apply to local PostgreSQL instance

---

### Phase 2: Repository Implementations (Priority: HIGH)

#### 2.1 DexcomLinkRepository
**File**: `Persistence/Repositories/DexcomLinkRepository.cs`

**Interface**: `Glyloop.Domain.Repositories.IDexcomLinkRepository`

**Methods**:
```csharp
Task<DexcomLink?> GetByIdAsync(Guid linkId, CancellationToken ct)
  → _context.DexcomLinks.FindAsync(linkId, ct)

Task<IReadOnlyList<DexcomLink>> GetByUserIdAsync(UserId userId, CancellationToken ct)
  → Where(l => l.UserId == userId).ToListAsync(ct)

Task<DexcomLink?> GetActiveByUserIdAsync(UserId userId, CancellationToken ct)
  → Where(l => l.UserId == userId && l.TokenExpiresAt > DateTimeOffset.UtcNow)
    .FirstOrDefaultAsync(ct)

Task<IReadOnlyList<DexcomLink>> GetLinksNeedingRefreshAsync(CancellationToken ct)
  → Where(l => l.TokenExpiresAt < DateTimeOffset.UtcNow.AddHours(1))
    .ToListAsync(ct)
    // For background refresh job (post-MVP)

void Add(DexcomLink link)
  → _context.DexcomLinks.Add(link)

void Remove(DexcomLink link)
  → _context.DexcomLinks.Remove(link)
```

**Design Notes**:
- Constructor injects `GlyloopDbContext`
- No `SaveChangesAsync()` in repository (handled by UnitOfWork)
- Use `AsNoTracking()` for read-only queries where appropriate

#### 2.2 EventRepository
**File**: `Persistence/Repositories/EventRepository.cs`

**Interface**: `Glyloop.Domain.Repositories.IEventRepository`

**Methods**:
```csharp
Task<Event?> GetByIdAsync(Guid eventId, CancellationToken ct)
  → _context.Events.FindAsync(eventId, ct)
  → EF Core handles polymorphic loading

Task<IReadOnlyList<Event>> GetByUserIdAsync(
    UserId userId, 
    EventType? type, 
    DateTimeOffset? from, 
    DateTimeOffset? to, 
    CancellationToken ct)
  → Query with optional filters
  → OrderByDescending(e => e.EventTime)

Task<PagedResult<Event>> GetPagedAsync(
    UserId userId,
    DateTimeOffset from,
    DateTimeOffset to,
    int page,
    int pageSize,
    CancellationToken ct)
  → Use Skip/Take for pagination
  → Return PagedResult with items + total count

Task<int> CountByUserIdAsync(
    UserId userId,
    DateTimeOffset from,
    DateTimeOffset to,
    EventType? type,
    CancellationToken ct)
  → CountAsync with filters

void Add(Event @event)
  → _context.Events.Add(@event)

void Remove(Event @event)
  → _context.Events.Remove(@event)
  → Note: MVP has immutable events, remove may be for admin/cleanup only
```

**Design Notes**:
- TPT polymorphism handled automatically by EF Core
- No explicit type casting needed when querying
- Use `OfType<FoodEvent>()` if need to filter by specific subtype

---

### Phase 3: Unit of Work & Domain Events (Priority: HIGH)

#### 3.1 UnitOfWork Implementation
**File**: `Persistence/UnitOfWork.cs`

**Interface**:
```csharp
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task<bool> SaveEntitiesAsync(CancellationToken ct = default);
}
```

**Implementation Strategy**:
```csharp
public async Task<bool> SaveEntitiesAsync(CancellationToken ct)
{
    // 1. Get all tracked aggregates with domain events
    var aggregatesWithEvents = _context.ChangeTracker
        .Entries<AggregateRoot<Guid>>()
        .Where(e => e.Entity.DomainEvents.Any())
        .Select(e => e.Entity)
        .ToList();

    // 2. Collect all domain events before saving
    var domainEvents = aggregatesWithEvents
        .SelectMany(a => a.DomainEvents)
        .ToList();

    // 3. Clear domain events from aggregates
    foreach (var aggregate in aggregatesWithEvents)
    {
        aggregate.ClearDomainEvents();
    }

    // 4. Save changes to database
    await _context.SaveChangesAsync(ct);

    // 5. Dispatch domain events via MediatR
    foreach (var domainEvent in domainEvents)
    {
        await _mediator.Publish(domainEvent, ct);
    }

    return true;
}
```

**Dependencies**:
- Inject `GlyloopDbContext` and `IMediator` (MediatR)
- Add MediatR NuGet package to Infrastructure project

**Design Notes**:
- Domain events dispatched AFTER successful persistence (eventual consistency)
- Failures in event handlers don't roll back database transaction
- For transactional consistency, could wrap in `IDbContextTransaction` and dispatch events within transaction

---

### Phase 4: Services & Cross-Cutting Concerns (Priority: HIGH)

#### 4.1 Token Encryption Service
**File**: `Services/TokenEncryptionService.cs`

**Interface**:
```csharp
public interface ITokenEncryptionService
{
    byte[] Encrypt(string plaintext);
    string Decrypt(byte[] ciphertext);
}
```

**Implementation**:
- Use ASP.NET Core Data Protection API (`IDataProtectionProvider`)
- Purpose string: `"Glyloop.DexcomTokens"`
- Key storage: File system (for Docker), Azure Key Vault (production)
- Configure in DI: `.AddDataProtection().PersistKeysToFileSystem(path)`

**Design Notes**:
- Encryption happens before domain aggregate creation
- Decryption happens after retrieval from repository
- Keys persisted to Docker volume for production

#### 4.2 System Time Provider
**File**: `Services/SystemTimeProvider.cs`

**Interface**: `Glyloop.Domain.Common.ITimeProvider`

**Implementation**:
```csharp
public class SystemTimeProvider : ITimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
```

**Test Implementation** (for tests):
```csharp
public class FakeTimeProvider : ITimeProvider
{
    public DateTimeOffset UtcNow { get; set; } = DateTimeOffset.UtcNow;
}
```

---

### Phase 5: External API Integration (Priority: MEDIUM)

#### 5.1 Dexcom API Client
**File**: `Services/DexcomApiClient.cs`

**Interface**:
```csharp
public interface IDexcomApiClient
{
    Task<OAuthTokenResponse> ExchangeCodeForTokensAsync(
        string authorizationCode, 
        CancellationToken ct);
    
    Task<OAuthTokenResponse> RefreshTokenAsync(
        string refreshToken, 
        CancellationToken ct);
    
    Task<GlucoseReadingsResponse> GetGlucoseReadingsAsync(
        string accessToken,
        DateTimeOffset startTime,
        DateTimeOffset endTime,
        CancellationToken ct);
}
```

**Configuration**:
- Base URL: `https://sandbox-api.dexcom.com` (from appsettings)
- Client ID and Client Secret: from configuration/secrets
- OAuth endpoints: `/v2/oauth2/token`
- Data endpoints: `/v3/users/self/egvs` (estimated glucose values)

**Resilience with Polly**:
```csharp
services.AddHttpClient<IDexcomApiClient, DexcomApiClient>()
    .AddTransientHttpErrorPolicy(policy => 
        policy.WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
            onRetry: (outcome, timespan, retryCount, context) =>
            {
                _logger.LogWarning("Retry {RetryCount} after {Delay}s", retryCount, timespan.TotalSeconds);
            }))
    .AddTransientHttpErrorPolicy(policy =>
        policy.CircuitBreakerAsync(5, TimeSpan.FromMinutes(1)));
```

**Rate Limiting** (429 handling):
- Implement exponential backoff with jitter (per PRD: 2x up to 30 min)
- Store retry state in-memory or distributed cache
- Return `Result<T>` with appropriate error for rate limit

**Response Models**:
```csharp
public record OAuthTokenResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    string TokenType);

public record GlucoseReadingsResponse(
    List<GlucoseReading> Records);

public record GlucoseReading(
    DateTimeOffset SystemTime,
    DateTimeOffset DisplayTime,
    int Value,
    string Unit,
    string Trend);
```

#### 5.2 Dexcom Sync Service (Background)
**File**: `Services/DexcomSyncService.cs`

**Purpose**: Poll Dexcom API every 5 minutes for new glucose readings

**Implementation** (for post-MVP):
- Use `IHostedService` or Hangfire for background job
- Query for all active `DexcomLink` entities
- For each, fetch recent glucose readings (last 30 minutes)
- Store in glucose readings table
- Handle token refresh if needed

**MVP**: Skip background service, fetch on-demand when user views dashboard

---

### Phase 6: Identity Configuration (Priority: HIGH)

#### 6.1 ASP.NET Core Identity Setup
**File**: `Identity/IdentityConfiguration.cs` (or in DependencyInjection.cs)

**Configuration**:
```csharp
services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
{
    // Password policy per PRD
    options.Password.RequiredLength = 12;
    options.Password.RequireDigit = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    
    // Sign-in settings
    options.SignIn.RequireConfirmedEmail = false; // MVP: no email verification
    options.SignIn.RequireConfirmedAccount = false;
    
    // User settings
    options.User.RequireUniqueEmail = true;
    options.User.AllowedUserNameCharacters = 
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    
    // Lockout (optional)
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
})
.AddEntityFrameworkStores<GlyloopDbContext>()
.AddDefaultTokenProviders();
```

**Design Notes**:
- Uses `ApplicationUser` (Guid-based) instead of default IdentityUser (string-based)
- Domain layer references users via `UserId` value object (Guid wrapper)
- Infrastructure provides `UserManager<ApplicationUser>` and `SignInManager<ApplicationUser>` for upper layers
- JWT configuration and cookie settings belong in API layer, not Infrastructure

---

### Phase 7: Dependency Injection & Configuration (Priority: HIGH)

#### 7.1 Infrastructure DI Extension
**File**: `DependencyInjection.cs`

**Structure**:
```csharp
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        AddPersistence(services, configuration);
        
        // Identity
        AddIdentity(services, configuration);
        
        // Services
        AddServices(services, configuration);
        
        // External APIs
        AddExternalServices(services, configuration);
        
        return services;
    }
    
    private static void AddPersistence(
        IServiceCollection services, 
        IConfiguration configuration)
    {
        services.AddDbContext<GlyloopDbContext>(options =>
        {
            options.UseNpgsql(
                configuration.GetConnectionString("Default"),
                npgsqlOptions =>
                {
                    npgsqlOptions.EnableRetryOnFailure(3);
                    npgsqlOptions.CommandTimeout(30);
                });
            
            // Logging (development only)
            if (configuration.GetValue<bool>("Logging:EnableSensitiveDataLogging"))
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }
        });
        
        // Repositories
        services.AddScoped<IDexcomLinkRepository, DexcomLinkRepository>();
        services.AddScoped<IEventRepository, EventRepository>();
        
        // Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();
    }
    
    private static void AddIdentity(
        IServiceCollection services,
        IConfiguration configuration)
    {
        // ASP.NET Core Identity configuration (see Phase 6.1)
    }
    
    private static void AddServices(
        IServiceCollection services,
        IConfiguration configuration)
    {
        // Time Provider
        services.AddSingleton<ITimeProvider, SystemTimeProvider>();
        
        // Token Encryption
        services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo("/app/keys")) // Docker volume
            .SetApplicationName("Glyloop");
        
        services.AddScoped<ITokenEncryptionService, TokenEncryptionService>();
    }
    
    private static void AddExternalServices(
        IServiceCollection services,
        IConfiguration configuration)
    {
        // Dexcom API Client with Polly (see Phase 5.1)
        services.AddHttpClient<IDexcomApiClient, DexcomApiClient>(client =>
        {
            client.BaseAddress = new Uri(configuration["Dexcom:BaseUrl"]!);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        })
        .AddPolicyHandler(GetRetryPolicy())
        .AddPolicyHandler(GetCircuitBreakerPolicy());
    }
}
```

#### 7.2 Configuration Files
**appsettings.json** (Infrastructure-related settings):
```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Database=glyloop;Username=postgres;Password=<secret>"
  },
  "Dexcom": {
    "BaseUrl": "https://sandbox-api.dexcom.com",
    "ClientId": "<from-secrets>",
    "ClientSecret": "<from-secrets>",
    "RedirectUri": "https://localhost:5001/auth/dexcom/callback"
  }
}
```

**Docker secrets/environment variables**:
- `ConnectionStrings__Default`
- `Dexcom__ClientId`
- `Dexcom__ClientSecret`

---

### Phase 8: Glucose Readings Storage (Priority: MEDIUM)

#### 8.1 GlucoseReading Entity (Read Model)
**File**: `Persistence/Entities/GlucoseReading.cs`

**Purpose**: Store imported Dexcom CGM data for charting and TIR calculations

**Structure**:
```csharp
public class GlucoseReading
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DateTimeOffset SystemTime { get; set; }    // UTC timestamp
    public DateTimeOffset DisplayTime { get; set; }   // Local timezone
    public int ValueMgDl { get; set; }                // Glucose in mg/dL
    public string? Trend { get; set; }                // Arrow direction
    public DateTimeOffset ImportedAt { get; set; }
}
```

**Configuration**:
```csharp
public class GlucoseReadingConfiguration : IEntityTypeConfiguration<GlucoseReading>
{
    public void Configure(EntityTypeBuilder<GlucoseReading> builder)
    {
        builder.ToTable("GlucoseReadings");
        
        builder.HasKey(g => g.Id);
        
        builder.Property(g => g.SystemTime)
            .IsRequired()
            .HasColumnType("timestamptz");
        
        builder.Property(g => g.ValueMgDl)
            .IsRequired();
        
        // Composite index for efficient time-series queries
        builder.HasIndex(g => new { g.UserId, g.SystemTime });
        
        // Unique constraint to prevent duplicates
        builder.HasIndex(g => new { g.UserId, g.SystemTime })
            .IsUnique();
    }
}
```

**Repository**:
```csharp
public interface IGlucoseReadingRepository
{
    Task<IReadOnlyList<GlucoseReading>> GetByUserIdInRangeAsync(
        Guid userId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct);
    
    Task AddRangeAsync(IEnumerable<GlucoseReading> readings, CancellationToken ct);
    
    Task<GlucoseReading?> GetClosestToTimeAsync(
        Guid userId,
        DateTimeOffset targetTime,
        TimeSpan tolerance,
        CancellationToken ct);
}
```

**Design Notes**:
- This is a read model, not a domain aggregate
- Imported data, not created by users
- Heavy read, minimal write (5-minute polling)
- Consider partitioning by date for large datasets (future optimization)

---

## Implementation Priority Summary

### Must-Have for MVP (Phase 1-4, 6-7)
1. ✅ DbContext with PostgreSQL
2. ✅ Entity configurations (DexcomLink, Event hierarchy, Identity)
3. ✅ Initial migration
4. ✅ Repository implementations (DexcomLink, Event)
5. ✅ Unit of Work with domain event dispatching
6. ✅ Token encryption service
7. ✅ System time provider
8. ✅ ASP.NET Core Identity configuration
9. ✅ Dependency injection setup

### Should-Have for MVP (Phase 5, 8)
10. ⏳ Dexcom API client (OAuth flow)
11. ⏳ On-demand CGM data fetch (when user views dashboard)

### Post-MVP
12. 🔮 Glucose readings storage and repository
13. 🔮 Background sync service (5-minute polling)
14. 🔮 Materialized views for complex queries
15. 🔮 Caching layer (Redis)
16. 🔮 Health checks for database and Dexcom API
17. 🔮 Logging and telemetry (Serilog, Application Insights)
18. 🔮 Database connection pooling optimization

---

## Testing Strategy

### Integration Tests
- Repository operations against test PostgreSQL database
- Entity configurations (can EF map correctly?)
- Migration application (up and down)
- Token encryption/decryption round-trip
- Unit of Work event dispatching

### Contract Tests
- Dexcom API client with mocked HTTP responses
- OAuth flow (authorization code exchange, token refresh)
- Glucose reading retrieval

### Test Database
- Use Docker container for PostgreSQL
- Apply migrations in test setup
- Seed with test data
- Clean up after tests

---

## Deliverables Checklist

- [ ] `ApplicationUser.cs` entity extending `IdentityUser<Guid>`
- [ ] `GlyloopDbContext.cs` with DbSets and Identity integration
- [ ] `ApplicationUserConfiguration.cs` for custom user properties
- [ ] Entity configurations for all aggregates and entities (DexcomLink, Event hierarchy)
- [ ] Value converters for domain Value Objects
- [ ] Initial EF Core migration
- [ ] `DexcomLinkRepository.cs` implementing `IDexcomLinkRepository`
- [ ] `EventRepository.cs` implementing `IEventRepository`
- [ ] `GlucoseReadingRepository.cs` implementing `IGlucoseReadingRepository`
- [ ] `UnitOfWork.cs` with domain event dispatching
- [ ] `TokenEncryptionService.cs` using Data Protection API
- [ ] `SystemTimeProvider.cs` implementing `ITimeProvider`
- [ ] `DexcomApiClient.cs` with Polly resilience policies
- [ ] `DependencyInjection.cs` wiring up all services
- [ ] `appsettings.json` with connection strings and Dexcom configuration
- [ ] Integration tests for repositories
- [ ] Migration tested on local PostgreSQL

---

## Success Criteria

✅ **Phase 1-4 Complete When**:
- All domain aggregates can be persisted and retrieved
- ApplicationUser with Guid keys integrated with Identity tables
- Migrations create correct table structure in PostgreSQL
- Repositories pass integration tests
- Domain events are dispatched after persistence
- Unit of Work coordinates transactions

✅ **Phase 5-8 Complete When**:
- Can exchange OAuth code for Dexcom tokens
- Can refresh expired Dexcom tokens
- Can fetch glucose readings
- Token encryption/decryption works end-to-end
- ASP.NET Identity with ApplicationUser ready for upper layers

---

**Next Actions**: Start with Phase 1 (DbContext and Entity Configurations), then Phase 2 (Repositories), then Phase 3 (Unit of Work).