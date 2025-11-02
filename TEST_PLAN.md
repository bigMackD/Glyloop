# GLYLOOP - COMPREHENSIVE TEST PLAN

## 1. Introduction and Testing Objectives

### 1.1 Project Purpose
Glyloop is a desktop-first web application for adults living with Type 1 Diabetes (T1D) who use Dexcom CGM. The application enables users to view current glucose levels, log events (meals, insulin, exercises, notes), and analyze their impact on an interactive time-series chart.

### 1.2 Testing Objectives
1. **Functional Correctness**: Verify all features work according to MVP specifications
2. **Data Integrity**: Ensure medical data (CGM, events) is accurate and immutable
3. **Security**: Protect sensitive data (tokens, passwords, user information)
4. **Performance**: Enable 5-minute polling without delays, responsive UI
5. **Reliability**: Graceful error handling, robust retry logic
6. **Compliance**: Alignment with HIPAA and privacy requirements

---

## 2. Test Scope

### 2.1 In-Scope Components

**Backend:**
- Domain Layer: Value Objects, Aggregates, Domain Events, Invariants
- Application Layer: Command Handlers, Query Handlers, Validators
- API Layer: Controllers, routing, error handling, response mapping
- Infrastructure Layer: Repositories, DbContext, Identity, External APIs

**Frontend:**
- Auth Components: Login, Register, Session Management
- Dashboard Components: CGM Chart, Event Modal, History Panel, TIR Summary
- Settings Components: Dexcom Linking, Account Preferences
- Services: API integration, state management
- Guards & Interceptors: Auth protection, CSRF handling

**Integration:**
- OAuth flow (Dexcom)
- JWT token handling
- httpOnly cookie management
- CSRF protection
- API endpoints end-to-end

### 2.2 Out-of-Scope Components
- Demo mode
- Dedicated sync status page
- 7-day backfill workflow
- Event editing/deletion/backdating
- Nightscout integration (post-MVP)
- Mobile apps & PWA
- Advanced accessibility
- Full observability dashboards

### 2.3 Excluded Scenarios
- Performance optimization (beyond MVP baseline)
- Load testing (planned post-MVP)
- Disaster recovery (planned post-MVP)
- Full CI/CD pipeline (planned post-MVP)

---

## 3. Types of Tests to be Performed

### 3.1 Unit Tests

#### Backend - Domain Layer
- **Value Objects**: Carbohydrate, InsulinDose, ExerciseDuration, NoteText, TirRange, UserId
  - Valid/invalid construction
  - Boundary values (0, min, max, +1)
  - Structural equality
  - Error returns via Result<T>

- **Aggregates**: DexcomLink, Event (FoodEvent, InsulinEvent, ExerciseEvent, NoteEvent)
  - Factory methods (Create)
  - Behavior methods (RefreshTokens, Unlink)
  - Domain events raised at correct transitions
  - Invariant enforcement
  - Identity-based equality

#### Backend - Application Layer
- **Command Handlers**: RegisterCommand, LoginCommand, CreateFoodEventCommand, LinkDexcomCommand
  - Success scenarios
  - Validation failures
  - Duplicate prevention (email)
  - Authorization checks

- **Query Handlers**: GetEventByIdQuery, ListEventsQuery, GetDexcomStatusQuery, GetEventOutcomeQuery
  - Correct data retrieval
  - Filtering & pagination
  - User isolation (cannot access other users' data)
  - +2h outcome edge cases

- **Validators**: RegisterValidator, CreateEventValidator
  - Valid inputs pass
  - Invalid inputs fail with correct error codes
  - Boundary testing (password >= 12 chars, carbs 0-300, etc.)

#### Backend - Services
- **JwtTokenService**: Token generation, validation, expiration
- **IdentityService**: Credential validation, account lockout
- **DexcomService**: OAuth token exchange, refresh logic
- **EncryptionService**: Token encryption/decryption

#### Frontend - Components (Jest)
- **LoginFormComponent**: Form validation, error display, submit handler
- **RegisterFormComponent**: Password validation (min 12), email format, terms
- **AddEventModalComponent**: Event type selection, field validation, submit
- **TirRangeFormComponent**: Lower < Upper validation, default 70-180
- **ChartComponent**: Canvas rendering, time range selection

#### Frontend - Services (Jest)
- **AuthApiService**: Login/register/logout/refresh calls
- **EventsService**: CRUD operations for events
- **ChartDataService**: Data transformation, series generation
- **DexcomApiService**: Status checks, link/unlink calls

### 3.2 Integration Tests

#### Backend - Database + EF Core
- Event creation persists to PostgreSQL
- Paged event retrieval works correctly
- DexcomLink token encryption/decryption
- Cascade deletes (if user deleted)
- Constraint violations (unique email)
- TimeZone handling (DateTimeOffset)

#### Backend - API Controllers + Handlers
- Register endpoint → CreateUserCommand → Domain → DB
- Login endpoint → ValidateCredentials → JWT generation → Cookies
- CreateFoodEvent endpoint → FoodEvent aggregate → Domain event → DB
- ListEvents endpoint → Query with filters → Paged response
- LinkDexcom endpoint → OAuth token exchange → Token storage
- GetEventOutcome endpoint → +2h calculation

#### API Security
- JWT token validation at controller level
- CSRF token requirement for POST/DELETE
- CORS headers correctly set
- Security headers (X-Frame-Options, X-Content-Type-Options, etc.)
- HttpOnly cookie behavior
- SameSite=Lax enforcement

#### Frontend - API Integration
- Auth interceptor adds credentials to requests
- 401 triggers token refresh
- CSRF token included in headers for state-changing calls
- Error handling displays user-friendly messages
- Session validation on app init

### 3.3 End-to-End Tests (E2E)

#### Happy Path Scenarios
1. **User Registration & Login**
   - User registers with valid email & password (≥12 chars)
   - User logs in with credentials
   - Session is established (cookies set)
   - Dashboard loads with user info

2. **Dexcom Linking**
   - User clicks "Link Dexcom"
   - Redirected to Dexcom OAuth
   - User approves authorization
   - Tokens stored securely
   - Dashboard shows "Linked" status

3. **Event Creation**
   - User opens "Add Event" modal
   - Selects "Food" type
   - Enters carbs (valid value)
   - Submits → Event created & visible in history
   - Chart updates if within time range

4. **+2h Outcome Display**
   - User creates Food event at time T
   - System calculates T+120 min
   - If CGM sample exists at T±5 min, outcome shows glucose value
   - If no sample, shows "N/A" with tooltip

5. **TIR Calculation**
   - Chart displays with default TIR range (70-180)
   - User adjusts range in Settings
   - TIR percentage recalculates
   - History shows correct % for active window

#### Error Scenarios
1. **Invalid Credentials**: Login fails with "Invalid email or password"
2. **Duplicate Email**: Registration fails with "Email already exists"
3. **Future Event Time**: Event creation rejected with "Event cannot be in future"
4. **Invalid Carbs**: Form shows "Carbs must be 0-300g"
5. **Session Expired**: 401 redirects to login
6. **Network Error**: Retry logic triggered, user sees "Reconnecting..." banner
7. **Dexcom Token Expired**: Background refresh triggered, user not interrupted
8. **Permission Denied**: User cannot access other user's data (403 equivalent)

### 3.4 Performance Tests

#### Backend
- ListEvents with 1000+ records: < 500ms response time
- Chart data generation for 24h with 100+ events: < 300ms
- JWT token validation: < 50ms
- +2h outcome lookup: < 100ms

#### Frontend
- Chart rendering with 500+ points: < 1s initial, < 300ms on range change
- Event modal open/close: < 200ms
- History panel scroll/pagination: smooth (60 FPS target)
- Tab switches: < 200ms

### 3.5 Security Tests

#### Authentication & Authorization
- JWT token only in httpOnly, secure, SameSite cookies
- Token claims verified (exp, sub, email)
- Refresh token rotation on use
- No token exposure in URL, logs, or UI
- CSRF token required for POST/DELETE/PUT
- Rate limiting on /auth/login (max 5 attempts per IP in 15 min)

#### Data Isolation
- User A cannot view/modify User B's events (403)
- User A cannot see User B's Dexcom link status
- Pagination doesn't leak data from other users

#### Password Policy
- Minimum 12 characters enforced
- No plaintext storage (bcrypt/ASP.NET Core Identity)
- Account lockout after 5 failed login attempts
- Password reset (if implemented)

#### Dexcom Token Security
- Access token encrypted at rest (AES-256)
- Refresh token encrypted at rest
- Token never logged in plain text
- Token revocation on unlink

#### Security Headers
- X-Frame-Options: DENY (clickjacking protection)
- X-Content-Type-Options: nosniff
- X-XSS-Protection: 1; mode=block
- Referrer-Policy: no-referrer
- Strict-Transport-Security: max-age=31536000 (HTTPS only)

#### Input Validation
- XSS prevention (sanitize event notes)
- SQL injection prevention (EF Core parameterized queries)
- XXE prevention (if XML parsing used)
- LDAP injection prevention (if directory used)

### 3.6 Regression Tests

#### Critical Path
- User can login after failed login attempt
- Event creation doesn't duplicate (idempotency)
- Token refresh doesn't break active session
- Unlink and re-link Dexcom works
- Chart time range changes don't lose data

#### Previous Bug Fixes
- If bug in event validation reported: add regression test
- If CORS issue occurred: re-verify CORS headers
- If datetime offset caused issues: test with multiple timezones

### 3.7 Error Handling Tests

#### API Error Responses
- 400 Bad Request: Invalid input, validation errors
- 401 Unauthorized: Missing/invalid token
- 403 Forbidden: Insufficient permissions
- 404 Not Found: Resource doesn't exist
- 409 Conflict: Email already exists, duplicate resource
- 500 Internal Server Error: Unhandled exception, logged with trace ID

#### Frontend Error Handling
- Network errors show toast "Connection error, retrying..."
- 401 redirects to login with "Session expired"
- 400 displays form validation errors inline
- 500 shows "Something went wrong" banner
- User can dismiss/retry errors

#### Timeout & Retry Logic
- Dexcom API timeouts (5min polling with exponential backoff)
- Database connection timeout recovery
- Frontend request timeout: retry once, then fail

### 3.8 Compatibility Tests

#### Frontend Browsers
- Chrome/Chromium latest
- Firefox latest
- Safari latest (macOS/iOS)
- Edge latest

#### Backend .NET
- .NET 8 (official support)

#### Database
- PostgreSQL 13+

### 3.9 Data-Driven Tests (Parametrized)

#### Carbohydrate Validation
- Test values: -1, 0, 1, 149, 150, 299, 300, 301, 1000
- Expected: -1 fail, 0-300 pass, 301+ fail

#### Insulin Dose Validation
- Test values: -0.5, 0, 0.25, 0.5, 1.0, 5.0, 50, 100, 100.5, 101
- Expected: -0.5 fail, 0/0.5/1.0/50/100 pass, 0.25 fail (not 0.5 increment), 101 fail

#### Exercise Duration
- Test values: -1, 0, 1, 150, 299, 300, 301
- Expected: -1/0 fail, 1-300 pass, 301 fail

#### TIR Range
- Test values: (0,10), (50,50), (70,180), (70,181), (180,70), (1000,1001)
- Expected: (70,180) pass, others fail

#### Event Time Validation
- Now-1h: pass
- Now: pass
- Now+1s: fail (future)
- Now+1h: fail (future)

#### Date Range Filtering
- Filter events from last 7 days: pass
- Filter from tomorrow onwards: return empty
- Filter with inverted dates: return empty

### 3.10 State Management Tests

#### Frontend State
- User session state persists on page refresh
- Dexcom link state updates after link/unlink
- Chart range selection persists during session
- Event modal state resets after submit
- History filter state persists

#### Backend State
- DexcomLink aggregate state changes on token refresh
- Event aggregate is immutable after creation
- User identity persists in JWT claims

---

## 4. Test Scenarios for Key Functionalities

### 4.1 Registration and Login Scenarios

#### TC-AUTH-001: Successful Registration
- **Step 1**: Open registration form
- **Step 2**: Enter email `user@example.com`, password `SecurePass1234`
- **Step 3**: Click submit
- **Expected Result**: Account created, redirect to login, success message

#### TC-AUTH-002: Registration - Password Too Short
- **Step 1**: Registration form
- **Step 2**: Enter password `Short123` (< 12 chars)
- **Step 3**: Click submit
- **Expected Result**: Error "Password must be at least 12 characters"

#### TC-AUTH-003: Registration - Duplicate Email
- **Step 1**: Email `user@example.com` already exists
- **Step 2**: Attempt to register with this email
- **Expected Result**: 409 error "Email already registered"

#### TC-AUTH-004: Successful Login
- **Step 1**: Open login form
- **Step 2**: Enter existing email and password
- **Step 3**: Click submit
- **Expected Result**: Logged in, redirect to dashboard, cookies set

#### TC-AUTH-005: Login - Invalid Password
- **Step 1**: Correct email, wrong password
- **Expected Result**: 401 error "Invalid credentials"

#### TC-AUTH-006: Login - Account Locked
- **Step 1**: 5 failed login attempts
- **Step 2**: 6th attempt
- **Expected Result**: 401 error "Account locked out"

#### TC-AUTH-007: Token Refresh
- **Step 1**: Access token expires (15 min)
- **Step 2**: User makes API request
- **Expected Result**: Token refreshed automatically, request succeeds

#### TC-AUTH-008: Logout
- **Step 1**: Logged-in user
- **Step 2**: Click "Logout"
- **Expected Result**: Cookies deleted, redirect to login

### 4.2 Dexcom Linking Scenarios

#### TC-DEXCOM-001: Successful Link
- **Step 1**: Dashboard, click "Link Dexcom"
- **Step 2**: Redirect to Dexcom OAuth
- **Step 3**: Login to Dexcom Sandbox
- **Step 4**: Approve access
- **Step 5**: Callback to application
- **Expected Result**: Status shows "Linked", tokens encrypted in DB

#### TC-DEXCOM-002: Unlink Dexcom
- **Step 1**: Dexcom is linked
- **Step 2**: Click "Unlink"
- **Expected Result**: Status shows "Not linked", tokens deleted

#### TC-DEXCOM-003: Token Expiry Detection
- **Step 1**: Token expires at
- **Step 2**: Polling attempts to fetch data
- **Expected Result**: Refresh token trigger, new tokens set

#### TC-DEXCOM-004: Backoff Strategy (429)
- **Step 1**: Dexcom returns 429 (rate limit)
- **Step 2**: System retry with exponential backoff (2x jitter, max 30 min)
- **Expected Result**: Eventual success or user notified

#### TC-DEXCOM-005: Invalid Authorization Code
- **Step 1**: OAuth code is invalid or expired
- **Step 2**: Link attempt
- **Expected Result**: 400 error "Invalid authorization code"

### 4.3 Event Logging Scenarios

#### TC-EVENT-001: Log Food Event
- **Step 1**: Dashboard, click "Add Event" → "Food"
- **Step 2**: Enter 45g carbs, meal tag "Breakfast", absorption "Normal"
- **Step 3**: Set time to now
- **Step 4**: Submit
- **Expected Result**: Event created, visible in history, chart updated

#### TC-EVENT-002: Insulin Event
- **Step 1**: "Add Event" → "Insulin"
- **Step 2**: Enter 10.5 units, type "Fast", delivery "Pen"
- **Step 3**: Submit
- **Expected Result**: Event created, overlay on chart

#### TC-EVENT-003: Exercise Event
- **Step 1**: "Add Event" → "Exercise"
- **Step 2**: Enter 30 min, type "Running", intensity "Vigorous"
- **Step 3**: Submit
- **Expected Result**: Event created

#### TC-EVENT-004: Note Event
- **Step 1**: "Add Event" → "Note"
- **Step 2**: Enter "Feeling stressed", submit
- **Expected Result**: Event created

#### TC-EVENT-005: Validation - Carbs Out of Range
- **Step 1**: Food event, enter 350g carbs
- **Expected Result**: Error "Carbs must be 0-300g"

#### TC-EVENT-006: Validation - Future Event Time
- **Step 1**: Event time = now + 1 hour
- **Expected Result**: Error "Event cannot be in future"

#### TC-EVENT-007: Event Is Immutable
- **Step 1**: Event created
- **Step 2**: Attempt to edit (if UI has option)
- **Expected Result**: No edit option, event locked

#### TC-EVENT-008: Insulin Dose - Invalid Increment (0.25 units)
- **Step 1**: Enter 10.25 units (not 0.5 increment)
- **Expected Result**: Error "Dose must be in 0.5 unit increments"

### 4.4 +2h Outcome Scenarios

#### TC-OUTCOME-001: Outcome Available
- **Step 1**: Food event at T, CGM sample at T+120 min
- **Step 2**: Click event, check outcome
- **Expected Result**: Outcome shows glucose value (e.g., 145 mg/dL)

#### TC-OUTCOME-002: Outcome N/A (No Data)
- **Step 1**: Food event at T, NO CGM sample at T±5 min
- **Expected Result**: Outcome shows "N/A" with explanatory tooltip

#### TC-OUTCOME-003: Strict ±5 Minute Tolerance
- **Step 1**: Food event at T=10:00, CGM sample at T=10:24:59 (exactly +5min)
- **Expected Result**: Outcome accepted (within tolerance)

#### TC-OUTCOME-004: Outside Tolerance
- **Step 1**: Food event at T=10:00, CGM sample at T=10:25:01 (outside tolerance)
- **Expected Result**: Outcome N/A

### 4.5 TIR Calculation Scenarios

#### TC-TIR-001: Default Range (70-180)
- **Step 1**: Dashboard open
- **Expected Result**: TIR summary shows % in range 70-180

#### TC-TIR-002: Custom Range
- **Step 1**: Settings, change TIR to 80-160
- **Step 2**: Dashboard
- **Expected Result**: TIR % recalculated for 80-160

#### TC-TIR-003: TIR for Active Window
- **Step 1**: Chart in 1h mode
- **Expected Result**: TIR shown for last 1h, not all data

#### TC-TIR-004: Boundary Values
- **Step 1**: TIR range (0, 1000)
- **Expected Result**: Pass (all glucose in range)

#### TC-TIR-005: Invalid Range
- **Step 1**: TIR range (180, 70) - lower > upper
- **Expected Result**: Error "Lower bound must be less than upper"

### 4.6 Event History Scenarios

#### TC-HISTORY-001: Paginated List
- **Step 1**: 50+ events in system
- **Step 2**: History panel, default page 1 (20 per page)
- **Expected Result**: 20 events shown, pagination controls visible

#### TC-HISTORY-002: Type Filtering
- **Step 1**: Filter by type "Food"
- **Expected Result**: Only Food events shown

#### TC-HISTORY-003: Date Range Filtering
- **Step 1**: Filter from "2 days ago" to "yesterday"
- **Expected Result**: Only events in this range shown

#### TC-HISTORY-004: Combined Filters
- **Step 1**: Type = "Insulin", Date = last 7 days
- **Expected Result**: Insulin events from last 7 days shown

#### TC-HISTORY-005: Empty Results
- **Step 1**: Filter with no matches
- **Expected Result**: "No events found" message

#### TC-HISTORY-006: Sorting
- **Step 1**: Events sorted by time (newest first)
- **Expected Result**: Correct chronological order

### 4.7 Chart Scenarios

#### TC-CHART-001: Fixed Time Ranges
- **Step 1**: Dashboard chart
- **Step 2**: Select 1h, 3h, 5h, 8h, 12h, 24h buttons
- **Expected Result**: Chart zooms correctly, data updates, events show/hide

#### TC-CHART-002: Crosshair Interaction
- **Step 1**: Hover over chart
- **Expected Result**: Crosshair appears, value tooltip shown

#### TC-CHART-003: Y-Axis Clamp [50, 350]
- **Step 1**: CGM data includes values 30 (below min) and 400 (above max)
- **Expected Result**: Y-axis clamped 50-350, data points rendered correctly

#### TC-CHART-004: Breaks in Data (No Polling)
- **Step 1**: 2-hour gap in Dexcom data
- **Expected Result**: Break shown on chart (gap, not connected line)

#### TC-CHART-005: Event Overlays
- **Step 1**: Food event in chart
- **Expected Result**: Food icon/marker shown at event time

---

## 5. Test Environment

### 5.1 Environments

#### Development
- Backend: localhost:5000 (ASP.NET dev server)
- Frontend: localhost:4200 (Angular dev server)
- Database: PostgreSQL local instance
- Dexcom API: Sandbox (test credentials)

#### Staging
- Backend: staging-api.glyloop.dev
- Frontend: staging.glyloop.dev
- Database: PostgreSQL staging instance
- Dexcom API: Sandbox

#### Production (Post-MVP)
- Backend: api.glyloop.app
- Frontend: app.glyloop.app
- Database: PostgreSQL production (encrypted)
- Dexcom API: Production (after user approval)

### 5.2 Hardware Requirements

#### Backend Testing
- CPU: 2+ cores
- RAM: 4GB minimum
- Storage: 10GB (for DB snapshots)

#### Frontend Testing
- Modern laptop/desktop
- Browsers: Chrome, Firefox, Safari, Edge

#### Load Testing (Future)
- VM instance with 8+ cores, 16GB RAM
- JMeter or k6 for load simulation

### 5.3 Test Data

#### User Accounts
- Admin user: admin@test.glyloop.dev / AdminPass1234
- Test user: user@test.glyloop.dev / UserPass1234
- Additional users for multi-user testing

#### Dexcom Sandbox Credentials
- Client ID: [stored in secure config]
- Client Secret: [stored in secure config]
- Test account: dexcom-sandbox@test.com

#### Event Test Data
- Pre-populated 30 days of CGM data
- 20-30 events per day (varied types)
- Some events with +2h outcomes, some without
- Timezone test data (UTC, EST, PST, etc.)

### 5.4 Tools and Frameworks

#### Backend
- xUnit / Fact & Theory for unit tests
- Moq for mocking
- EF Core In-Memory for database tests
- FluentAssertions for test assertions
- TestContainers for database tests

#### Frontend
- Jest for unit tests
- TypeScript/Angular test utilities
- Cypress or Playwright for E2E (future)

#### API Testing
- Postman collections for manual testing
- REST Client VS Code extension

#### Automation
- GitHub Actions for CI/CD
- Pytest for infrastructure tests (Docker, Kubernetes post-MVP)

---

## 6. Testing Tools

### 6.1 Unit Testing Tools

| Tool | Purpose | Framework |
|------|---------|-----------|
| xUnit | Unit test framework | Backend (.NET) |
| Moq | Mocking library | Backend (.NET) |
| FluentAssertions | Readable assertions | Backend (.NET) |
| Jest | Unit test framework | Frontend (Angular) |
| @angular/core/testing | Angular testing utilities | Frontend (Angular) |
| TypeScript | Type-safe testing | Frontend |

### 6.2 Integration Testing Tools

| Tool | Purpose |
|------|---------|
| TestContainers | Docker containers for integration tests |
| Npgsql | PostgreSQL ADO.NET provider |
| HttpClient | API testing (direct calls) |
| Cypress | E2E browser automation (future) |

### 6.3 Performance Testing Tools

| Tool | Purpose |
|------|---------|
| k6 | Load testing, API performance |
| Chrome DevTools | Frontend performance profiling |
| SQL Server Profiler | Database query analysis |
| Application Insights | APM & logging (future) |

### 6.4 Security Testing Tools

| Tool | Purpose |
|------|---------|
| OWASP ZAP | Vulnerability scanning |
| Burp Suite Community | Manual security testing |
| npm audit | Frontend dependency vulnerabilities |
| dotnet analyzer | Backend code analysis |

### 6.5 Code Quality Tools

| Tool | Purpose |
|------|---------|
| SonarQube | Code coverage & quality analysis |
| Code Climate | Technical debt tracking |
| ESLint | Frontend linting |
| StyleCop | Backend code style |

---

## 7. Test Schedule

### 7.1 Testing Phases

#### Phase 1: Development (Current)
- **Duration**: Week 1-2 (October 2024)
- **Activities**:
  - Backend: Domain layer unit tests (DDD aggregates)
  - Backend: Repository integration tests
  - Frontend: Component unit tests (forms, modals)
  - Manual E2E smoke tests

#### Phase 2: Feature Verification
- **Duration**: Week 3-4
- **Activities**:
  - Complete backend API testing (all endpoints)
  - Complete frontend feature testing
  - Security testing (JWT, cookies, CSRF)
  - +2h outcome calculation verification
  - TIR calculation validation

#### Phase 3: Integration & E2E
- **Duration**: Week 5-6
- **Activities**:
  - Full E2E user workflows
  - Dexcom OAuth flow testing
  - Multi-user scenarios
  - Error handling & edge cases
  - Cross-browser testing

#### Phase 4: Performance & Stress
- **Duration**: Week 7
- **Activities**:
  - Load testing (1000+ concurrent users)
  - Sustained polling test (24h)
  - Large dataset pagination
  - Memory leak detection

#### Phase 5: UAT & Sign-off
- **Duration**: Week 8
- **Activities**:
  - Product owner acceptance
  - User stories verification
  - Final regression testing
  - Production deployment readiness


---

## 8. Test Acceptance Criteria

### 8.1 Test Execution Criteria

- **Minimum Coverage**: 80% for Backend Domain/Application, 70% for Frontend
- **Pass Rate**: 100% of critical path tests before release
- **Defect Rate**: ≤5 defects per 1000 LOC (low severity tolerated)

### 8.2 Security Criteria

- **OWASP Top 10**: No critical/high findings before release
- **JWT Token Security**: Validated by security team
- **Data Encryption**: Access tokens encrypted at rest, verified
- **CORS**: Properly configured, no overly permissive origins

### 8.4 Defect Severity Classification

| Severity | Definition | Example | Fix Timeline |
|----------|-----------|---------|--------------|
| Critical | Application unusable | Login fails, app crashes | Immediate (same day) |
| High | Major feature broken | Event creation fails, chart doesn't load | 24 hours |
| Medium | Feature degraded | UI glitch, slow response | 3-5 days |
| Low | Minor issue | Typo, styling issue | Backlog |

### 8.5 Sign-off Criteria

- [ ] All critical path tests passed
- [ ] 0 critical / high defects remaining
- [ ] ≥80% code coverage (backend), ≥70% (frontend)
- [ ] Security audit completed with green flag
- [ ] Performance benchmarks met
- [ ] Product owner sign-off on UAT
- [ ] Deployment checklist completed

---

## 11. Additional: Post-MVP Testing Plans

## Appendices

### Appendix A: Test Environment Variables

```
# Backend
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://localhost:5000
DATABASE_CONNECTION_STRING=postgres://localhost/glyloop_test
JWT_SECRET_KEY=[test-key]
DEXCOM_CLIENT_ID=[test-client-id]
DEXCOM_CLIENT_SECRET=[test-secret]

# Frontend
NG_ENVIRONMENT=development
API_BASE_URL=http://localhost:5000/api
```

### Appendix B: Test Data Factory

```csharp
// Example: Create test user
var user = new UserFactory()
    .WithEmail("test@example.com")
    .WithPassword("TestPass1234")
    .Build();

// Example: Create test food event
var foodEvent = new FoodEventFactory()
    .WithCarbohydrates(45)
    .WithMealTag("Breakfast")
    .WithEventTime(DateTimeOffset.UtcNow.AddHours(-2))
    .Build();
```

### Appendix C: Mapping User Stories → Test Cases

| User Story | Acceptance Criteria | Test Cases |
|-----------|-------------------|-----------|
| US-001: Authenticate | Given valid credentials, when login, then signed in | TC-AUTH-004, TC-AUTH-007 |
| US-003: Link Dexcom | Given OAuth approval, when authorize, then linked | TC-DEXCOM-001 |
| US-005: Log Food | Given valid carbs, when submit, then event created | TC-EVENT-001, TC-OUTCOME-001 |
| US-007: View TIR | Given active window, when chart shown, then TIR calculated | TC-TIR-001, TC-TIR-003 |

---
