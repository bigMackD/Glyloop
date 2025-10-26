# UI Architecture for Glyloop

## 1. UI Structure Overview

- Desktop-first Angular 19 SPA with always-on dark mode.
- Protected AppShell wraps authenticated routes: `/dashboard`, `/settings/*`.
- Public routes: `/login`, `/register`.
- Redirect `/` → `/dashboard` if authenticated, else `/login`.
- Persistent "Add Event" trigger visible only on `/dashboard`.
- All API calls use `withCredentials: true`; single refresh attempt on 401; centralized error handling and ETag-aware caching.

## 2. View List

### Auth: Login
- View path: `/login`
- Main purpose: Authenticate existing users and start a secure session.
- Key information to display: Email/password inputs, lockout/error messages, link to Register.
- Key view components: Auth form, submit button, session-expired banner (if redirected), footer links.
- UX, accessibility, and security considerations:
  - Disable submit while pending; inline validation errors.
  - Keyboard-first navigation; proper labels and aria-describedby for errors.
  - Do not expose token in UI; rely on httpOnly cookie.

### Auth: Register
- View path: `/register`
- Main purpose: Create account per password policy (≥12 chars) and redirect to login.
- Key information to display: Email, password requirements, success prompt to log in.
- Key view components: Registration form, password strength hint, submit button.
- UX, accessibility, and security considerations:
  - Enforce min length 12; show validation inline; prevent weak passwords client-side.
  - Prevent autofill pitfalls; announce errors via aria-live.

### AppShell (Protected Layout)
- View path: Wrapper for `/dashboard`, `/settings/*`
- Main purpose: Provide global nav, status, and consistent layout.
- Key information to display: App nav, offline chip, session-expired banner, user menu.
- Key view components: Header, nav tabs, content outlet, notifications area.
- UX, accessibility, and security considerations:
  - Skip-to-content link; focus management on navigation; high-contrast dark theme.
  - Show offline indicator; guard protected routes; refresh-once logic.

### Dashboard
- View path: `/dashboard`
- Main purpose: Display CGM time-series, overlays, TIR%, and enable quick event logging, with an embedded History panel below the chart.
- Key information to display: Chart (1h/3h/5h/8h/12h/24h), TIR%, event overlays (Food/Insulin/Exercise/Notes), polling status, History with filters and results.
- Key view components: Range buttons, Chart.js graph with crosshair, TIR summary, Add Event trigger and modal, History panel (filters + virtualized list).
- UX, accessibility, and security considerations:
  - Compute ranges in UTC, render local time; round to minute.
  - Keyboard crosshair stepping; readable tooltips; ≥4.5:1 contrast.
  - Poll every 5 min; pause on hidden; backoff on 429/5xx; ETag caching.
  - Selecting an event in History re-centers the chart to ±30 minutes around that event and highlights its marker.

### Add Event Modal
- View path: Modal invoked from `/dashboard`
- Main purpose: Create Food, Insulin, Exercise, or Note events.
- Key information to display: Type tabs, validated inputs, current time default.
- Key view components: Type selector tabs, forms per type, submit/cancel, inline ProblemDetails mapping, success toast.
- UX, accessibility, and security considerations:
  - Trap focus; escape to close; submit disabled while pending.
  - Bounds: carbs 0–300; insulin 0–100 (0.5 steps); duration 1–300; note ≤500.
  - No edits/backdating; on success, add marker without recentering.

### History Panel (Embedded)
- View path: Embedded under `/dashboard` chart
- Main purpose: Browse past events with filters and jump the chart to selected timestamps.
- Key information to display: Filter bar (date range, type/tag), event list, selection details.
- Key view components: Sticky filters, virtualized list, details drawer/dialog, jump-to-time interaction with chart.
- UX, accessibility, and security considerations:
  - Server paging if available; otherwise date-bounded queries.
  - Keyboard navigation in list; sufficient hit targets; accessible table mode toggle.
  - Preserve scroll position and filters when chart range changes.

### Settings: Account
- View path: `/settings/account`
- Main purpose: Manage user preferences (e.g., TIR range per PRD).
- Key information to display: Current TIR bounds, inputs to adjust.
- Key view components: Form controls for TIR range, save/cancel.
- UX, accessibility, and security considerations:
  - Validate range; show immediate preview; announce save success/errors.

### Settings: Data Sources
- View path: `/settings/data-sources`
- Main purpose: Manage Dexcom link status; link/unlink flow.
- Key information to display: Link status, last sync, actions to link/unlink.
- Key view components: Status card, Link button, Unlink confirmation with optional purge toggle (if supported), toasts.
- UX, accessibility, and security considerations:
  - Start OAuth from here; return to same view post-callback; announce results.

### Settings: Dexcom Callback
- View path: `/settings/data-sources/dexcom/callback`
- Main purpose: Complete OAuth; exchange code and update status.
- Key information to display: Progress/Result state; auto-redirect back to Data Sources.
- Key view components: Progress spinner, success/error state, timed redirect.
- UX, accessibility, and security considerations:
  - Handle missing/invalid code; show actionable errors.

### Settings: Display & Units
- View path: `/settings/display`
- Main purpose: Manage display-related settings (dark mode fixed; TIR range if not in Account).
- Key information to display: Theme note (always dark), TIR range controls (if placed here).
- Key view components: Info text, form controls.
- UX, accessibility, and security considerations:
  - Maintain consistent contrast and motion preferences.

### Settings: System Info
- View path: `/settings/system`
- Main purpose: Show minimal read-only environment info.
- Key information to display: App version, environment, health status link.
- Key view components: Info cards.
- UX, accessibility, and security considerations:
  - Ensure content is readable and non-intrusive.

## 3. User Journey Map

- First-time visit (unauthenticated): `/` → `/login` → submit credentials → set secure cookie → redirect to `/dashboard`.
- Link Dexcom: `/settings/data-sources` → start OAuth → `/settings/data-sources/dexcom/callback` → POST code to link → success toast → back to Data Sources with updated status.
- View glucose: `/dashboard` → select range (default 3h) → fetch `/api/chart/data` and `/api/chart/tir` with UTC `start`/`end` → inspect via crosshair; poll every 5 min.
- Log event: On `/dashboard` click Add Event → select type and enter fields → submit to `/api/events/{type}` → show success toast and place overlay marker → remain on current range.
- Review history: On `/dashboard` use the embedded History panel → set filters (`from`, `to`, `type`, `tag`) → list loads (paged or date-bounded) → select row → chart centers to ±30 minutes around event.
- Session handling: Any 401 → interceptor tries `/api/auth/refresh` once → on failure, banner + redirect to `/login`.

## 4. Layout and Navigation Structure

- AppShell header: Logo/title, nav tabs (Dashboard, Settings), offline chip, user menu, notifications area.
- Navigation:
  - Public: `/login`, `/register` via simple header/footer.
  - Protected: Tabs link to `/dashboard`, `/settings` (default to `/settings/data-sources`).
  - Deep links: `/settings/data-sources/dexcom/callback` for OAuth completion.
- Routing guards and behaviors:
  - AuthGuard for protected routes; redirect `/` based on auth state.
  - Resolver-free approach; data loads in components with services and caching.

## 5. Key Components

- AppShell: Protected layout with header, nav, outlet, notifications, offline chip, session banner.
- RangeSelector: Buttons for 1h/3h/5h/8h/12h/24h; emits UTC window.
- GlucoseChart: Chart.js instance with crosshair, overlays, and breaks; adheres to [50, 350] mg/dL clamps.
- TIRSummary: Computes and displays TIR% for active window from `/api/chart/tir`.
- AddEventModal: Tabbed form for Food, Insulin (fast/long visuals), Exercise, Notes; client validation and ProblemDetails mapping.
- HistoryPanel: Embedded panel combining filters and virtualized list; selection emits `eventTimeUtc` and `eventId`.
- HistoryFilters: Sticky filter bar for date range and type/tag; emits query params.
- EventListVirtual: Virtualized list/table with accessible navigation and selection.
- DashboardHistoryInteraction: Selection from History causes chart to re-center ±30 minutes, highlight the corresponding overlay marker, and open details.
- DexcomStatusCard: Shows link status and actions; handles unlink confirmation and optional purge.
- NotificationService + Interceptor: Centralized error categories (Reconnect/Slow down/Check connection/Report bug), debounced retries, rate-limit countdown.
- AuthGuard + AuthInterceptor: `withCredentials`, single refresh attempt on 401, redirect/banner on failure.
- CachingInterceptor: ETag/If-None-Match support; URL+params cache with 304 handling; `shareReplay` dedupe; explicit invalidation on range/filter changes and event creation.
- AccessibilityUtilities: Skip link, focus management helpers, aria-live region manager, keyboard crosshair controls.
