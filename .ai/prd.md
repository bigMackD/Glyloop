# Product Requirements Document (PRD) - Glyloop
## 1. Product Overview
Glyloop is a desktop-first web application for adults living with Type 1 Diabetes who use Dexcom CGM. The MVP focuses on a tight daily loop: view current CGM glucose, log events (food, insulin, exercise, notes), and review their impact on an interactive time-series chart. MVP ships with Dexcom Sandbox as the only data source; Nightscout integration and data export arrive post-MVP. The app is containerized and delivered via Docker with a lightweight deployment setup.

Key constraints and principles:
- Platform: Web app only, desktop-first, always-on dark mode.
- Data source: Dexcom Sandbox only in MVP, 5-minute polling.
- Time and units: All timestamps stored in UTC; glucose stored and displayed in mg/dL for MVP.
- Security and auth: ASP.NET Core Identity with JWT for SPA; no email verification in MVP; session management via secure httpOnly cookies and CSRF protection for state changes.
- Data handling: Strict event schemas; events are immutable post-create in MVP; no edits/backdating/deletes.
- Charting: Chart.js time-series with fixed ranges, crosshair, gaps shown as breaks, dynamic y-clamp [50, 350] mg/dL, no smoothing.
- Operations: Multi-stage Dockerfiles for API and Web, docker-compose orchestration, basic health endpoint.

## 2. User Problem
People with T1D need a fast, reliable way to see current glucose, log key daily events, and understand the short-term impact of those events without complexity. Existing solutions can be fragmented or heavy; Glyloop MVP focuses on a minimal, high-clarity experience centered on the immediate glucose timeline and simple, validated event logging.

## 3. Functional Requirements
3.1 Data source and sync
- Dexcom Sandbox OAuth/PKCE link flow.
- Polling cadence every 5 minutes.
- Backoff strategy on 429/5xx: exponential 2x with jitter up to 30 minutes.
- Token handling: proactive refresh at 80% of TTL; encrypted at rest; rotation every 90 days or upon 401 bursts; revoke and relink capability.
- On successful link, fetch a recent window (e.g., latest 24 hours) to populate the chart without a dedicated backfill workflow.

3.2 Data model and rules
- Storage: timestamps in UTC; render in local timezone; persist event_time_utc and display_timezone.
- Units: canonical mg/dL; display in mg/dL (no unit switching in MVP).
- Event types and fields:
  - Food: carbs_g (required), meal_tag, absorption_hint, note.
  - Insulin fast/long: insulin_units (required), preparation, delivery, timing, note.
  - Exercise: type, duration_min, intensity, start_time.
  - Notes: free text up to 500 chars.
- Numeric bounds: carbs 0–300 g; insulin 0–100 U in 0.5 U steps; duration 1–300 min.
- Controlled vocabularies with Other (free text) fallback.
- +2h outcome rule: anchor at event_time + 120 minutes; show a CGM value only if a sample exists within ±5 minutes; otherwise show N/A with an explanatory tooltip and an ≈2h badge. No meal grouping.

3.3 Event lifecycle and history
- Create: modal form with type selector defaulting to Food; required field validation.
- No edits, deletes, or backdating in MVP; events are immutable once created.
- History view remains available for browsing logged events and jumping the chart to event timestamps.

3.4 Chart and analysis
- Chart ranges: 1h, 3h, 5h, 8h, 12h, 24h; default 3h on first visit.
- Interactions: crosshair with exact value/time; tooltips for glucose and overlays; gaps shown as breaks; dynamic y-clamp [50, 350] mg/dL; no smoothing.
- Overlays: display Food, Insulin (fast/long with distinct icons/colors and vertical stacking), Exercise, Notes.
- Time in range (TIR): default target 70–180 mg/dL; one editable range; compute and display TIR% for the active window; exclude missing intervals from denominator.

3.5 Settings and IA
- Tabs: Account; Data Sources; Display and Units; System Info (read-only elements may be minimized in MVP).
- Data Sources: link/unlink Dexcom; on unlink retain already-synced data but stop new imports; optional data purge.
- Display and Units: fixed mg/dL in MVP; one editable target range for TIR.
- System Info: minimal read-only environment info (no dedicated user-facing system flags page in MVP).

3.6 Authentication and security
- Authentication: ASP.NET Core Identity + JWT for SPA; no email verification in MVP.
- Password policy: minimum 12 characters, no additional complexity requirements.
- Sessions and storage: secure httpOnly cookies, SameSite=Lax; CSRF for state-changing calls; do not store tokens in localStorage.
- Legal: not medical advice acknowledgment, minimal Terms and Privacy provided by the project owner, 16+ age gate.

3.7 Deployment and CI
- Docker in MVP: multi-stage Dockerfile for API (.NET 8) and Web (Angular built and served by Nginx); optional reverse proxy; docker-compose with healthchecks and restart policies; configuration via environment variables or mounted secrets.
- CI quality gates: lint checks; unit test coverage ≥ 70% for domain logic; provider contract tests with mocked Dexcom; one Playwright e2e for the happy path (login → link Dexcom → view chart → add event).
- Feature flags: EnableDexcom is on; Nightscout remains off until post-MVP.

## 4. Product Boundaries
In scope for MVP:
- Dexcom Sandbox linking, 5-minute polling, resilient sync.
- Event logging for Food, Insulin, Exercise, Notes with validation and overlays.
- +2h food outcome per single event with strict ±5-minute tolerance.
- TIR calculation for active window with one editable range.
- History view with date range and type/tag filters.
- Dockerized deployment with compose.
- Security posture with Identity + JWT, password policy, token encryption at rest.

Out of scope for MVP:
- Demo mode and related UI.
- Dedicated sync status chip and detailed status page.
- Initial 7-day backfill workflow and user-triggered load older pages.
- Duplicate-prevention UX and server idempotency exposure.
- Event editing, deletion, and backdating.
- System Info flags page.
- Lightweight observability story and alerting dashboards.
- Units conversion UI and persistence of preferred chart ranges.
- Nightscout integration and data exports.
- Advanced accessibility work beyond maintaining adequate contrast.
- Mobile apps and offline/PWA functionality.
- Full support desk workflow.

## 5. User Stories
US-001  
Title: Authenticate into Glyloop  
Description: As a registered user, I want to sign in securely so that I can access my dashboard.  
Acceptance Criteria:  
- Given valid credentials, when I submit the login form, then I am signed in and redirected to the dashboard.  
- Given invalid credentials, when I submit, then I see an inline error and remain on the login page.  
- Sessions are stored via secure httpOnly cookies and protected by CSRF for state-changing calls.

US-002  
Title: Register a new account  
Description: As a new user, I want to create an account so I can use Glyloop.  
Acceptance Criteria:  
- When I provide a unique email and a password with at least 12 characters, the account is created without email verification in MVP.  
- If the password is shorter than 12 characters, I see a validation error and cannot register.  
- After registration, I am prompted to log in.

US-003  
Title: Link Dexcom Sandbox  
Description: As a user, I want to link my Dexcom Sandbox account so Glyloop can import my CGM data.  
Acceptance Criteria:  
- The app uses OAuth/PKCE for Dexcom Sandbox.  
- On success, the Data Sources screen shows linked status.  
- On failure, I see an actionable error.

US-004  
Title: View glucose chart  
Description: As a user, I want to view glucose over time with interactive inspection.  
Acceptance Criteria:  
- I can switch ranges between 1h/3h/5h/8h/12h/24h; default is 3h on first visit.  
- I can move a crosshair to inspect exact glucose values and timestamps.  
- Gaps are displayed as breaks; the y-axis clamps to [50, 350] mg/dL; no smoothing is applied.

US-005  
Title: Configure target range  
Description: As a user, I want to set a target glucose range to personalize TIR.  
Acceptance Criteria:  
- I can set one editable TIR range, with default 70–180 mg/dL.  
- TIR% for the active chart window is displayed.  
- Missing CGM intervals are excluded from TIR denominator.

US-006  
Title: Add a Food event  
Description: As a user, I want to log carbs with optional meal details so I can see the impact on glucose.  
Acceptance Criteria:  
- The Add Event modal defaults to Food with required carbs_g and optional meal_tag, absorption_hint, note.  
- Validation: carbs_g must be between 0 and 300.  
- On save, the food event appears on the chart and in History.

US-007  
Title: Add an Insulin event (fast or long-acting)  
Description: As a user, I want to log insulin doses to track their timing relative to glucose.  
Acceptance Criteria:  
- Required insulin_units; optional preparation, delivery, timing, note.  
- Validation: insulin_units must be 0–100 in 0.5 U steps.  
- Fast and long-acting use distinct icons/colors and appear on the chart.

US-008  
Title: Add an Exercise event  
Description: As a user, I want to log exercise to review its impact.  
Acceptance Criteria:  
- Required fields: type, duration_min; optional intensity, start_time.  
- Validation: duration_min must be 1–300.  
- Exercise overlay appears on the chart and in History.

US-009  
Title: Add a Note event  
Description: As a user, I want to leave a free-text note for context.  
Acceptance Criteria:  
- I can add up to 500 characters of text.  
- The note appears on the chart and in History.

US-010  
Title: See +2h outcome for Food  
Description: As a user, I want to quickly learn post-meal impact.  
Acceptance Criteria:  
- Tapping a Food event shows a glucose value if a CGM reading exists within ±5 minutes of +120 minutes post-event.  
- If no reading exists, N/A is displayed with an explanatory tooltip and an ≈2h badge.

US-011  
Title: History view with filters and jump-to-time  
Description: As a user, I want to browse and locate past events efficiently.  
Acceptance Criteria:  
- I can filter by date range and by type/tag.  
- Clicking any row opens event details and moves the chart to that time.

US-012  
Title: Manage Dexcom link and unlink  
Description: As a user, I want control over my data connections.  
Acceptance Criteria:  
- Data Sources shows link status.  
- Unlinking stops new imports but retains prior data; I may optionally purge.

US-013  
Title: Handle sync and API errors with clear actions  
Description: As a user, I want to understand what went wrong and what I can do.  
Acceptance Criteria:  
- Errors are mapped to four buckets: Reconnect (auth), Slow down (rate limit), Check connection (network), Report bug (parsing).  
- Retry actions are debounced and rate-limited.

US-014  
Title: Secure access and password policy  
Description: As a user, I want my account protected with reasonable requirements.  
Acceptance Criteria:  
- Passwords must be at least 12 characters; if shorter, registration is blocked with a clear validation error.  
- Sessions use secure httpOnly cookies; CSRF protection is applied to state-changing endpoints.  
- No email verification or transactional emails in MVP.

US-015  
Title: Containerized deployment  
Description: As an operator, I want the app to run in containers consistently across environments.  
Acceptance Criteria:  
- API and Web have multi-stage Dockerfiles; docker-compose can run both plus optional reverse proxy.  
- Containers expose a health endpoint; services restart as configured; secrets are provided via environment variables or mounted files.

US-016  
Title: Visual differentiation of insulin types  
Description: As a user, I want to distinguish between fast-acting and long-acting insulin on the chart.  
Acceptance Criteria:  
- Fast-acting and long-acting injections have distinct icons and colors; stacked vertically when collisions occur; popovers show dose, preparation, timing, and note.

US-017  
Title: Data retention and privacy acknowledgment  
Description: As a user, I want clarity about data use and retention.  
Acceptance Criteria:  
- On first use, I acknowledge a not medical advice notice and minimal Terms/Privacy.  
- Data retention defaults are documented in the app; token storage is encrypted at rest.

## 6. Success Metrics
Primary success signals for MVP readiness and usefulness:
- Qualitative: Users can reliably link Dexcom Sandbox, see up-to-date glucose in the selected range, log events with validation, and view +2h outcomes or clear N/A feedback.
- Operational: Sync follows the 5-minute cadence with acceptable latency; containers pass health checks and restart policies; basic stability across supported desktop browsers.
