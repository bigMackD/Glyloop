# Glyloop API Layer Implementation Plan

## 1. Endpoint Overview

### Authentication Endpoints

- **POST** `/api/auth/register` - Register new user account (201)
- **POST** `/api/auth/login` - Login with credentials, sets JWT httpOnly cookie (200)
- **POST** `/api/auth/logout` - Logout, clears cookie (200)
- **POST** `/api/auth/refresh` - Refresh JWT token from cookie (200)

### Account Management Endpoints

- **GET** `/api/account/preferences` - Get user TIR preferences (200, 401)
- **PUT** `/api/account/preferences` - Update TIR preferences (200, 400, 401)

### Dexcom Integration Endpoints

- **GET** `/api/dexcom/authorize` - Redirect to Dexcom OAuth (302)
- **GET** `/api/dexcom/callback` - OAuth callback handler (302)
- **POST** `/api/dexcom/link` - Link Dexcom with auth code (201, 400, 401)
- **DELETE** `/api/dexcom/unlink` - Unlink Dexcom account (200, 401, 404)
- **GET** `/api/dexcom/status` - Get link status (200, 401)

### Event Endpoints

- **POST** `/api/events/food` - Log food event (201, 400, 401)
- **POST** `/api/events/insulin` - Log insulin event (201, 400, 401)
- **POST** `/api/events/exercise` - Log exercise event (201, 400, 401)
- **POST** `/api/events/note` - Log note event (201, 400, 401)
- **GET** `/api/events` - List events with filters (200, 400, 401)
- **GET** `/api/events/{id}` - Get event by ID (200, 401, 404)
- **GET** `/api/events/{id}/outcome` - Get +2h food outcome (200, 401, 404)

### Chart Endpoints

- **GET** `/api/chart/data` - Get glucose data for time range (200, 400, 401)
- **GET** `/api/chart/tir` - Get Time in Range stats (200, 400, 401)

### Health Endpoint

- **GET** `/health` - Health check for container orchestration (200, 503)

## 2. Request Details

### Contract Structure

```
Glyloop.API/
  Contracts/
    Auth/
      RegisterRequest.cs
      LoginRequest.cs
      LoginResponse.cs
      RefreshResponse.cs
    Account/
      UpdatePreferencesRequest.cs
      PreferencesResponse.cs
    Dexcom/
      LinkDexcomRequest.cs
      LinkDexcomResponse.cs
      DexcomStatusResponse.cs
    Events/
      CreateFoodEventRequest.cs
      CreateInsulinEventRequest.cs
      CreateExerciseEventRequest.cs
      CreateNoteEventRequest.cs
      EventResponse.cs
      EventListResponse.cs
      EventOutcomeResponse.cs
    Chart/
      ChartDataResponse.cs
      TimeInRangeResponse.cs
    Common/
      ErrorResponse.cs
      ValidationErrorResponse.cs
      PagedResponse.cs
```

### Sample Contracts

**CreateFoodEventRequest**: Carbs (0-300g), MealTag, AbsorptionHint, Note (≤500 chars)

**LoginRequest**: Email, Password

**UpdatePreferencesRequest**: TirLowerBound (mg/dL), TirUpperBound (mg/dL)

### Validation Rules

- Email format validation
- Password minimum 12 characters
- Carbs: 0-300g, Insulin: 0-100U (0.5 increments), Duration: 1-300 min
- Date validation: no future events
- Required fields per event type

## 3. Response Details

### Success Responses

- **200 OK**: Read operations, successful updates
- **201 Created**: Resource creation with `Location` header
- **204 No Content**: Logout, delete operations

### Error Response Format

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred",
  "status": 400,
  "errors": {
    "CarbohydratesGrams": ["Must be between 0 and 300"]
  },
  "traceId": "00-abc123..."
}
```

### Domain Error Mapping

- `User.InvalidEmail` → 400 Bad Request
- `DexcomLink.TokenExpired` → 401 Unauthorized (trigger relink)
- `DexcomLink.LinkNotFound` → 404 Not Found
- `Event.EventInFuture` → 400 Bad Request
- `Event.InvalidCarbohydrates` → 400 Bad Request
- `Validation.Failed` → 400 Bad Request
- Unhandled exceptions → 500 Internal Server Error

## 4. Data Flow

### Request Pipeline

1. **CORS Middleware** - Validate origin (localhost:4200 dev, configurable prod)
2. **HTTPS Redirection** - Enforce secure transport
3. **Authentication Middleware** - Validate JWT from cookie
4. **CSRF Token Validation** - For state-changing operations (POST/PUT/DELETE)
5. **Controller** - Parse request, validate model
6. **Mapper** - Contract → Command/Query
7. **MediatR** - LoggingBehavior → ValidationBehavior → Handler
8. **Handler** - Business logic via Application layer
9. **Mapper** - Result → Contract
10. **Response** - Serialize JSON, set headers

### Authentication Flow

1. User submits credentials to `/api/auth/login`
2. `AuthController` creates `LoginCommand` via mapper
3. `LoginCommandHandler` validates credentials via `IIdentityService`
4. On success, generate JWT with claims (UserId, Email)
5. Set JWT in httpOnly cookie (Secure, SameSite=Lax, 15min expiry)
6. Set refresh token in separate httpOnly cookie (7 days expiry)
7. Return 200 with user info (no tokens in body)

## 5. Security Considerations

### Threats & Mitigations

**Threat: XSS Token Theft**

*Mitigation*: JWT in httpOnly cookies, never in localStorage/sessionStorage. CSP headers.

**Threat: CSRF Attacks**

*Mitigation*: Antiforgery tokens for POST/PUT/DELETE. SameSite=Lax cookies. Validate origin.

**Threat: SQL Injection**

*Mitigation*: EF Core parameterized queries, no raw SQL in MVP.

**Threat: Brute Force Login**

*Mitigation*: ASP.NET Identity lockout (5 attempts, 15min). Rate limiting on auth endpoints.

**Threat: Token Replay**

*Mitigation*: Short-lived access tokens (15min), refresh token rotation, token revocation list.

**Threat: Sensitive Data Exposure**

*Mitigation*: HTTPS only, encrypted tokens at rest (Dexcom), audit logging, no PII in logs.

**Threat: Mass Assignment**

*Mitigation*: Explicit contract models, no direct binding to domain entities.

**Threat: Unauthorized Access**

*Mitigation*: `[Authorize]` on all controllers except auth/health. User context from claims.

### Security Headers

- `X-Content-Type-Options: nosniff`
- `X-Frame-Options: DENY`
- `X-XSS-Protection: 1; mode=block`
- `Strict-Transport-Security: max-age=31536000`
- `Content-Security-Policy` (restrict script sources)

### Rate Limiting

- Auth endpoints: 5 requests/minute per IP
- Event creation: 30 requests/minute per user
- Chart data: 60 requests/minute per user

## 6. Error Handling

### Status Code Mapping

- **400 Bad Request**: Validation errors, domain rule violations
- **401 Unauthorized**: Missing/invalid JWT, expired token
- **403 Forbidden**: User lacks permission (future: resource ownership)
- **404 Not Found**: Resource doesn't exist (event, dexcom link)
- **409 Conflict**: Duplicate email on registration
- **422 Unprocessable Entity**: Business rule violation
- **429 Too Many Requests**: Rate limit exceeded
- **500 Internal Server Error**: Unhandled exceptions
- **503 Service Unavailable**: Database down, external API unavailable

### Error Categories (per PRD US-013)

1. **Reconnect**: 401 from Dexcom → "Please reconnect your Dexcom account"
2. **Slow Down**: 429 from Dexcom → "Too many requests, please wait"
3. **Check Connection**: Network errors → "Check your internet connection"
4. **Report Bug**: Parsing errors, 500 → "Something went wrong, please report"

### Exception Handling Middleware

```csharp
GlobalExceptionHandler : IExceptionHandler
- Catch all exceptions
- Log with correlation ID
- Map to appropriate status code
- Return structured ErrorResponse
- Never expose stack traces in production
```

### Validation Error Handling

```csharp
FluentValidation pipeline behavior (existing)
→ Returns Result.Failure with validation errors
→ Controller maps to 400 with ProblemDetails
```

## 7. Performance

### Optimization Strategies

- **Async/Await**: All I/O operations (database, HTTP)
- **No Tracking**: Read queries use `AsNoTracking()`
- **Response Caching**: Chart data with ETag (5min cache)
- **Compression**: Gzip/Brotli for responses
- **Connection Pooling**: DbContext scoped lifetime, EF connection pooling
- **Query Optimization**: Eager loading with `Include()` to avoid N+1

### Health Check

- Basic: API alive (HTTP 200)
- Database: PostgreSQL connection check
- Dexcom: Feature flag check (no live call)
- Response time: < 200ms

## 8. Implementation Steps

### Step 1: Project Setup

**Files**: `Glyloop.API.csproj`, `appsettings.json`

- Add packages: `Microsoft.AspNetCore.Authentication.JwtBearer`, `FluentValidation.AspNetCore`, `AspNetCore.HealthChecks.NpgSql`
- Configure appsettings sections: `JwtSettings`, `CorsSettings`, `RateLimiting`, `FeatureFlags`

### Step 2: Create Contract Models

**Location**: `Glyloop.API/Contracts/`

- Create request/response classes for all endpoints
- Add DataAnnotations for basic validation (fallback to FluentValidation)
- Use records for immutability
- Add XML documentation on all public contracts

### Step 3: Create Static Configuration Classes

**Files**:

- `Configuration/AuthenticationConfiguration.cs` - JWT setup
- `Configuration/AuthorizationConfiguration.cs` - Policies
- `Configuration/CorsConfiguration.cs` - CORS policy
- `Configuration/SwaggerConfiguration.cs` - OpenAPI/Swagger
- `Configuration/HealthCheckConfiguration.cs` - Health checks
- `Configuration/RateLimitingConfiguration.cs` - Rate limiting

Each class: static method `Add{Feature}(this IServiceCollection, IConfiguration)`

### Step 4: Create Mappers

**Location**: `Glyloop.API/Mapping/`

- `ContractMapper.cs` - Static methods to map contracts ↔ commands/queries
- `ResponseMapper.cs` - Map Result<T> → response contracts
- Keep pure functions, no dependencies
- Handle null/optional fields correctly

### Step 5: Implement Middleware

**Location**: `Glyloop.API/Middleware/`

- `GlobalExceptionHandler.cs` - Implements `IExceptionHandler` (.NET 8)
- `ValidationExceptionMiddleware.cs` - Catch FluentValidation exceptions (if needed)
- Middleware registration extensions in separate static class

### Step 6: Create API Controllers

**Location**: `Glyloop.API/Controllers/`

- `AuthController.cs` - Register, Login, Logout, Refresh
- `AccountController.cs` - Preferences management
- `DexcomController.cs` - OAuth flow, link/unlink, status
- `EventsController.cs` - CRUD operations for all event types
- `ChartController.cs` - Glucose data, TIR stats

**Controller Guidelines**:

- Inherit from `ControllerBase` (not `Controller`)
- Use `[ApiController]` attribute for automatic model validation
- Add `[Authorize]` at class level, `[AllowAnonymous]` on auth endpoints
- Rich XML documentation: `<summary>`, `<remarks>`, `<param>`, `<response>`
- Return `ActionResult<T>` for type safety
- Use `CreatedAtAction` for 201 responses with Location header
- Extract user ID from claims: `User.FindFirst(ClaimTypes.NameIdentifier)`

### Step 7: Implement Authentication Service

**Location**: `Glyloop.Infrastructure/Services/Identity/`

- `IdentityService.cs` - Implements `IIdentityService`
- `JwtTokenService.cs` - Generate/validate JWT tokens
- `ICurrentUserService.cs` interface in Application
- `CurrentUserService.cs` in API layer (accesses HttpContext)

### Step 8: Configure Program.cs

**File**: `Program.cs`

- Remove weather forecast sample
- Add Application layer: `builder.Services.AddApplication()`
- Register configuration classes in order
- Configure middleware pipeline in correct order
- Add CORS, Authentication, Authorization, HealthChecks, RateLimiting
- Map controllers: `app.MapControllers()`
- Configure Swagger with JWT support (AddSecurityDefinition)

### Step 9: Update appsettings.json

**File**: `appsettings.json`, `appsettings.Development.json`

```json
{
  "ConnectionStrings": { "Default": "..." },
  "JwtSettings": {
    "SecretKey": "env:JWT_SECRET_KEY",
    "Issuer": "https://localhost:5001",
    "Audience": "https://localhost:5001",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
  },
  "CorsSettings": {
    "AllowedOrigins": ["http://localhost:4200"],
    "AllowedMethods": ["GET", "POST", "PUT", "DELETE"],
    "AllowCredentials": true
  },
  "Dexcom": { ... },
  "RateLimiting": {
    "AuthEndpoints": { "PermitLimit": 5, "Window": 60 },
    "EventEndpoints": { "PermitLimit": 30, "Window": 60 },
    "ChartEndpoints": { "PermitLimit": 60, "Window": 60 }
  },
  "FeatureFlags": {
    "EnableDexcom": true,
    "EnableNightscout": false
  }
}
```

**Security**: Use environment variables or Azure Key Vault for secrets in production.

### Step 10: Add Health Checks

**File**: `Endpoints/HealthEndpoint.cs` or in `Program.cs`

- Add database health check: `AddNpgSql(connectionString)`
- Add custom health check for feature flags
- Map health endpoint: `app.MapHealthChecks("/health")`
- Return 200 if healthy, 503 if unhealthy

### Step 11: Testing & Validation

- Test all endpoints with Postman/REST Client
- Verify JWT cookie set correctly (Secure, HttpOnly, SameSite)
- Test CORS with Angular frontend
- Verify error responses match ProblemDetails format
- Test rate limiting on auth endpoints
- Verify 401 on protected endpoints without token
- Test Swagger UI with authentication

### Step 12: Documentation

- Generate OpenAPI spec from XML comments
- Ensure all endpoints have examples in Swagger
- Document rate limits and authentication flow
- Add README for API setup and configuration

## Key Files to Create

### API Layer (35+ files)

- `Contracts/` folder with 20+ contract classes
- `Controllers/` folder with 5 controller classes
- `Mapping/` folder with 2 mapper classes
- `Middleware/` folder with 2 middleware classes
- `Configuration/` folder with 6 configuration classes
- `Services/CurrentUserService.cs`
- Updated `Program.cs`

### Infrastructure Layer (2 files)

- `Services/Identity/IdentityService.cs` - Implement IIdentityService
- `Services/Identity/JwtTokenService.cs` - JWT generation/validation

### appsettings Updates

- Add JWT, CORS, RateLimiting, FeatureFlags sections

## Validation & Testing Checklist

- [ ] All endpoints return correct status codes
- [ ] JWT cookie set with correct flags (HttpOnly, Secure, SameSite)
- [ ] CSRF protection on POST/PUT/DELETE
- [ ] CORS allows Angular origin with credentials
- [ ] Validation errors return 400 with ProblemDetails
- [ ] Domain errors mapped to appropriate status codes
- [ ] Unhandled exceptions return 500 without exposing internals
- [ ] Rate limiting enforced on auth endpoints
- [ ] Health check returns database status
- [ ] Swagger UI loads with all endpoints documented
- [ ] OpenAPI spec includes XML documentation
- [ ] All secrets in environment variables/config