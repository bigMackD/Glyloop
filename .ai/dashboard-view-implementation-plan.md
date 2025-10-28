## View Implementation Plan – Dashboard

## 1. Overview
The Dashboard view provides a desktop-first, dark-mode interface that displays a CGM time-series chart with event overlays (Food, Insulin, Exercise, Notes), a Time-in-Range (TIR) summary, and an embedded History panel. It supports range switching (1h/3h/5h/8h/12h/24h), keyboard-accessible crosshair inspection, and quick event logging via an Add Event modal. The view polls every 5 minutes (pausing when hidden) with exponential backoff and optional ETag caching. All calculations use UTC, while times are rendered in the user’s local timezone.

## 2. View Routing
- Path: `/dashboard`
- Guard: Authenticated route; on 401 redirect to `/login`.

## 3. Component Structure
- `DashboardPageComponent`
  - `ChartToolbarComponent`
  - `CgmChartComponent`
    - `TirSummaryComponent` (can be placed inline next to toolbar on wide screens, or below on narrow)
  - Add Event button (plain button in header) + `AddEventModalComponent`
  - `HistoryPanelComponent`
    - `HistoryFilterBarComponent`
    - `HistoryVirtualListComponent`
    - `EventDetailsDrawerComponent`

## 4. Component Details

### DashboardPageComponent
- Purpose: Orchestrates data loading, polling, range selection, and coordination between chart and history.
- Main elements: page container, toolbar area, chart area, TIR summary, Add Event button/modal, history panel.
- Handled interactions:
  - Select chart range; trigger data and TIR fetch.
  - Polling start/stop (pause on hidden; manual resume if error/backoff).
  - When a History row is selected, center chart to ±30 minutes and highlight event.
  - Open Add Event modal; on successful create, refresh chart and history.
- Validation conditions:
  - Chart range must be one of: 1h|3h|5h|8h|12h|24h.
  - History pageSize must be ≤ 100; page ≥ 1.
  - Outcome requests only for Food events.
- Types:
  - Uses `ChartRange`, `ChartDataResponseDto`, `TimeInRangeResponseDto`, `PagedResponseDto<EventListItemDto>`, `EventResponseDto`, `EventOutcomeResponseDto`.
- Props: N/A (top-level route component).

### ChartToolbarComponent
- Purpose: Range selection buttons, polling status chip, timezone note.
- Main elements: segmented buttons (1h/3h/5h/8h/12h/24h), polling status indicator (OK/Paused/Retry in X), small text “All times local; range computed in UTC”.
- Handled interactions:
  - Click range button → emit `rangeChange(range: ChartRange)`.
  - Optional: click status chip → manual refresh.
- Validation conditions:
  - Disable active range button; keep others enabled.
- Types:
  - Input: `activeRange: ChartRange`, `pollState: PollState`.
  - Output: `rangeChange(range: ChartRange)`, `manualRefresh()`.
- Props (Inputs/Outputs): as above.

### CgmChartComponent
- Purpose: Renders CGM line series with gaps as breaks, y-clamp [50, 350] mg/dL, no smoothing, event overlays, and crosshair with keyboard stepping.
- Main elements: `canvas` for Chart.js; overlay layer for crosshair; legend of overlays with icons/colors.
- Handled interactions:
  - Mouse move/touch to move crosshair (snaps to nearest point); left/right arrow keys to step point-by-point; Shift+Arrow for 5-minute step; Enter on focused overlay marker opens details.
  - Click an overlay marker → emit `eventSelect(eventId)`.
- Validation conditions:
  - Ensure datasets use `spanGaps: false` to show breaks.
  - Clamp y-axis: suggestedMin=50, suggestedMax=350; no smoothing (tension=0; cubicInterpolationMode default).
  - Timeseries axis in local timezone; domain computed in UTC by parent.
- Types:
  - Input: `chartData: ChartDataResponseDto`, `highlightEventId?: string`, `range: ChartRange`.
  - Output: `eventSelect(eventId: string)`, `crosshairMove(timestampUtc: string)`.
- Props (Inputs/Outputs): as above.

### TirSummaryComponent
- Purpose: Displays TIR% for current window and counts in/out-of-range.
- Main elements: percentage badge, mini breakdown (in/above/below), displayed target range.
- Handled interactions: none (read-only); can expose info tooltip.
- Validation conditions:
  - Show denominator excluding missing intervals (backend-provided).
- Types:
  - Input: `tir: TimeInRangeResponseDto`.
  - Props: `tir`.

### AddEventModalComponent
- Purpose: Create Food, Insulin, Exercise, or Note events.
- Main elements: modal with tabs; forms per type; submit/cancel; inline validation messages.
- Handled interactions:
  - Submit form → POST to `/api/events/{type}`; on success close and emit `created(event: EventResponseDto)`.
  - Switch type tabs; all forms reset when tab changes.
- Validation conditions (per PRD):
  - Food: `carbs_g` required 0–300; optional `meal_tag`, `absorption_hint`, `note` (≤500 chars).
  - Insulin: `insulin_units` required 0–100 in 0.5U steps; optional `preparation`, `delivery`, `timing`, `note` (≤500).
  - Exercise: `type` required; `duration_min` 1–300; optional `intensity`, `start_time` (not future).
  - Note: text ≤ 500 chars.
  - No backdating: event time defaults to now; disallow past/future as required by API (MVP forbids backdating; future times invalid).
- Types:
  - Inputs: `open: boolean`.
  - Output: `close()`, `created(event: EventResponseDto)`.
- Props: as above.

### HistoryPanelComponent
- Purpose: Embedded panel to browse and filter events; jump chart to selected event and open details.
- Main elements: sticky filter bar, virtualized list, details drawer/dialog.
- Handled interactions:
  - Filter change → fetch events with filters.
  - Row click/keyboard Enter → emit `select(eventId)`.
  - Details open for selection; “Jump to time” triggers chart re-center to ±30 minutes.
- Validation conditions:
  - Keep `pageSize` ≤ 100; maintain date-bounded queries if needed; preserve filters and scroll on chart range change.
- Types:
  - Input: `initialFilters: HistoryFilters`, `selectedEventId?: string`.
  - Output: `select(eventId: string)`, `filtersChange(filters: HistoryFilters)`.
- Props: as above.

### HistoryFilterBarComponent
- Purpose: Controls filters: date range, type/tag, search (optional minimal), paging actions.
- Main elements: date range picker (UTC-aware), multiselect for event types/tags, apply/reset buttons.
- Handled interactions: change controls → emit `filtersChange` with debounced apply; reset.
- Validation conditions: date `from <= to`; clamp size to API limits.
- Types:
  - Input: `filters: HistoryFilters`.
  - Output: `filtersChange(filters: HistoryFilters)`, `reset()`.
- Props: as above.

### HistoryVirtualListComponent
- Purpose: Efficiently render many events using Angular CDK Virtual Scroll.
- Main elements: `cdk-virtual-scroll-viewport`, item renderer with large hit targets, optional table mode.
- Handled interactions: keyboard navigation (Up/Down moves focus/selection; Enter opens details).
- Validation conditions: maintain accessible row semantics (role="listbox"/"option" or table semantics in table mode).
- Types:
- Input: `items: EventListItemDto[]`, `selectedEventId?: string`.
- Output: `rowActivate(eventId: string)`.
- Props: as above.

### EventDetailsDrawerComponent
- Purpose: Shows full event details; for Food events, fetches +2h outcome.
- Main elements: side drawer or dialog with event fields, outcome section (value or N/A with tooltip and ≈2h badge).
- Handled interactions: close; when visible for Food, auto-fetch outcome; retry on demand.
- Validation conditions: outcome only for Food; handle 404 as N/A.
- Types:
  - Input: `event?: EventResponseDto`.
  - Output: `close()`.
- Props: as above.

## 5. Types

```ts
// Shared
export type ChartRange = '1h' | '3h' | '5h' | '8h' | '12h' | '24h';

export type EventType = 'Food' | 'Insulin' | 'Exercise' | 'Note';

export interface PagedResponseDto<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

// Chart
export interface GlucosePointDto {
  timestampUtc: string; // ISO 8601 UTC
  mgdl: number | null;  // null to create a gap/break
}

export interface OverlayEventMarkerDto {
  eventId: string;
  type: EventType;
  timestampUtc: string;
  icon: string;      // name or URL (frontend maps types to icons/colors)
  color: string;     // hex
}

export interface ChartDataResponseDto {
  glucose: GlucosePointDto[];
  overlays: OverlayEventMarkerDto[];
  rangeSeconds: number; // echoed by server or computed client
}

export interface TimeInRangeResponseDto {
  timeInRangePercentage: number; // 0..100
  totalReadings: number;
  readingsInRange: number;
  readingsBelowRange: number;
  readingsAboveRange: number;
  targetLowMgdl: number;   // e.g., 70
  targetHighMgdl: number;  // e.g., 180
}

// Events (list and details)
export interface EventListItemDto {
  eventId: string;
  type: EventType;
  timestampUtc: string;
  summary: string;    // e.g., "Food: 45g" or "Insulin: 3U (fast)"
  tags?: string[];
}

export interface EventResponseDto {
  eventId: string;
  type: EventType;
  timestampUtc: string;
  // Food
  carbs_g?: number;
  meal_tag?: string;
  absorption_hint?: string;
  note?: string;
  // Insulin
  insulin_units?: number; // 0..100 in 0.5 steps
  preparation?: string;
  delivery?: string;
  timing?: string;
  // Exercise
  exercise_type?: string;
  duration_min?: number;  // 1..300
  intensity?: string;
  start_time_utc?: string;
}

export interface EventOutcomeResponseDto {
  eventId: string;
  available: boolean;   // true if glucose exists within ±5 min of +120 min
  outcomeTimestampUtc?: string;
  glucoseMgdl?: number; // present only if available
}

// Create Event payloads
export interface CreateFoodEventRequestDto {
  carbs_g: number;
  meal_tag?: string;
  absorption_hint?: string;
  note?: string; // ≤500
}

export interface CreateInsulinEventRequestDto {
  insulin_units: number; // 0..100, step 0.5
  preparation?: string;
  delivery?: string;
  timing?: string;
  note?: string; // ≤500
}

export interface CreateExerciseEventRequestDto {
  type: string;
  duration_min: number; // 1..300
  intensity?: string;
  start_time_utc?: string; // not in future
}

export interface CreateNoteEventRequestDto {
  note: string; // ≤500
}

// View models
export interface HistoryFilters {
  fromDateUtc?: string; // ISO, start inclusive
  toDateUtc?: string;   // ISO, end inclusive
  types?: EventType[];
  page: number;
  pageSize: number; // ≤100
}

export type PollState =
  | { status: 'idle' }
  | { status: 'ok'; lastFetchedAt: Date }
  | { status: 'paused' }
  | { status: 'backoff'; nextRetryAt: Date; attempt: number }
  | { status: 'error'; message: string };
```

## 6. State Management
- Use Angular services with RxJS subjects/observables for view-scoped state.
- `DashboardStateService`
  - Holds `activeRange: ChartRange`, `selectedEventId?: string`, `pollState: PollState`.
  - Exposes `range$`, `selectedEventId$`, `pollState$`.
  - Methods: `setRange(range)`, `selectEvent(id)`, `setPollState(state)`.
- `ChartDataService`
  - Fetches `/api/chart/data` and `/api/chart/tir` by range; manages ETag (If-None-Match) if provided by server.
  - Polling: `start(range)`, `stop()`. Implements: 5 min interval; pause on hidden (Page Visibility API); `retryWhen` with exponential backoff (2^n with jitter, max 30 min).
  - Streams: `chartData$`, `tir$`.
- `EventsService`
  - Methods: `list(filters)`, `get(id)`, `getOutcome(id)`, `createFood`, `createInsulin`, `createExercise`, `createNote`.
  - Keeps ephemeral cache for current filters; exposes `historyPage$`.
- Keyboard focus and crosshair position are local to `CgmChartComponent` and not globally stored; emit `crosshairMove` for a11y announcements if needed.

## 7. API Integration
- GET `/api/chart/data?range={ChartRange}` → `ChartDataResponseDto`
  - 200: update chart; 400: show inline error; 401: redirect to login.
- GET `/api/chart/tir?range={ChartRange}` → `TimeInRangeResponseDto`
  - Same status handling as above.
- GET `/api/events?eventType={EventType?}&fromDate={ISO?}&toDate={ISO?}&page={n}&pageSize={n}` → `PagedResponseDto<EventListItemDto>`
  - 200: render list; 400: show filter error; 401: redirect to login.
- GET `/api/events/{id}` → `EventResponseDto`
  - 200 or 404; 404 displays “Event not found”.
- GET `/api/events/{id}/outcome` → `EventOutcomeResponseDto`
  - 200: if `available=false`, show N/A with tooltip; 404: treat as N/A + info message.
- POST `/api/events/food|insulin|exercise|note` (from `AddEventModalComponent`)
  - CSRF protected; send XSRF header via Angular `HttpClientXsrfModule`.
  - On 201: close modal, refresh chart and history; on 400 show validation messages; on 401 redirect to login.

Request/response TypeScript DTOs are defined in section 5. Convert date strings to `Date` on receipt, and always send UTC ISO strings when needed.

## 8. User Interactions
- Range button click → fetch new chart and TIR; preserve history filters/scroll.
- Mouse/keyboard crosshair movement → update visual indicator and accessible status text (aria-live) for the selected point.
- Click overlay marker → select event → open details drawer and highlight marker.
- History filter changes → debounced fetch; list preserves scroll on subsequent fetches; selection maintained if item still present.
- History row select/double-click/Enter → open details; “Jump to time” recenters chart to `event.timestamp ± 30 min` and highlights.
- Add Event button → open modal; after successful create → toast confirmation; refresh chart (keep same range) and history (preserve filters and page if possible; otherwise refetch first page including the new event if within range).
- Polling status chip → on error/backoff allows manual refresh; indicates next retry moment.

## 9. Conditions and Validation
- Chart range must be one of allowed values; invalid values are clamped to default `3h`.
- Timestamps: compute ranges in UTC; render ticks and tooltips in local time; round endpoints to minute.
- Y-axis: clamp suggested bounds [50, 350] mg/dL; do not smooth; show gaps as breaks (`mgdl: null`).
- History filters: ensure `fromDateUtc <= toDateUtc`; `pageSize ≤ 100`; `page ≥ 1`.
- Outcome: request only for Food events; treat 404 or `available=false` as N/A with explanatory tooltip and ≈2h badge.
- Add Event modal:
  - Food: `carbs_g` 0–300; note ≤ 500.
  - Insulin: `insulin_units` 0–100 in 0.5 steps; note ≤ 500.
  - Exercise: `duration_min` 1–300; `start_time_utc` not future.
  - Note: text ≤ 500.
  - No backdating: time defaults to now; controls disabled for past/future in MVP.

## 10. Error Handling
- 401: global interceptor redirects to `/login` and clears view state.
- 400 on queries/posts: show inline validation messages near the control that caused it (e.g., invalid filter or form field). Also show a non-blocking toast.
- Network errors/timeouts: map to “Check connection” bucket; show retry with debounce.
- 429/5xx: exponential backoff with jitter (max 30 min), surface “Slow down”/“Retrying in X” in polling status chip.
- Parsing/data shape issues: map to “Report bug”; show info banner and log to console with details.
- ETag present: on 304 (if server supports), keep current chart data and update poll state to OK.

## 11. Implementation Steps
1. Routing: add `/dashboard` route with auth guard.
2. Create services: `DashboardStateService`, `ChartDataService`, `EventsService` with DTOs from section 5.
3. Implement `ChartToolbarComponent` with range buttons and polling status chip.
4. Implement `CgmChartComponent` using Chart.js v4 timeseries:
   - Configure datasets with `spanGaps: false`, no smoothing, y clamp.
   - Draw overlay markers; implement crosshair (mouse/touch + keyboard) and accessible announcements.
5. Implement `TirSummaryComponent` bound to TIR stream.
6. Implement `HistoryPanelComponent` with `HistoryFilterBarComponent` and `HistoryVirtualListComponent` (CDK virtual scroll). Preserve filters and scroll.
7. Implement `EventDetailsDrawerComponent` with outcome fetch for Food.
8. Add a plain "Add Event" button in `DashboardPageComponent` and implement `AddEventModalComponent` with four forms; wire POST endpoints and CSRF.
9. Wire interactions in `DashboardPageComponent`:
   - Range changes trigger `ChartDataService` and TIR fetch.
   - History selection recenters chart to ±30 min and highlights marker.
   - On create event, refresh chart and history.
10. Polling logic: start on init; pause on document hidden; apply backoff on 429/5xx; optional ETag handling.
11. Accessibility: ensure keyboard navigation for crosshair and list; add aria roles/labels; ensure ≥4.5:1 contrast.
12. QA against user stories:
   - US-004: range switch, crosshair, gaps, y-clamp validated.
   - US-010: outcome rules implemented; N/A path verified.
   - US-011: filters, list navigation, jump-to-time verified.
13. Add lightweight unit tests for services and utility functions; basic component tests for toolbar and filters.


