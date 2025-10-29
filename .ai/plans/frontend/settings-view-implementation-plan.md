# View Implementation Plan — Settings

## 1. Overview
A single Settings view that hosts five sections: Account (TIR range), Data Sources (Dexcom link management), Dexcom Callback handler, Display & Units (read-only note; TIR is in Account), and System Info (read-only). The view loads current preferences and Dexcom link status, allows updating TIR bounds with validation and immediate preview, and manages the Dexcom OAuth link/unlink flow with clear feedback.

## 2. View Routing
- Primary route: `/settings`
  - In-view navigation shows the five sections without leaving the page (side nav or tabs). Each section has a fragment/child route for deep-linking.
- Child routes/anchors for direct access:
  - `/settings/account`
  - `/settings/data-sources`
  - `/settings/display`
  - `/settings/system`
- Dexcom OAuth frontend callback handler:
  - Backend currently redirects to `/dexcom-link?code=...`.
  - Implement route alias: `/dexcom-link` → callback handler component.
  - Optional alias (if backend redirectUri is updated later): `/settings/data-sources/dexcom/callback` → same handler.

## 3. Component Structure
- `SettingsView` (page)
  - `SettingsSectionNav` (side navigation or tabs)
  - `AccountPreferencesSection`
    - `TirRangeForm`
  - `DataSourcesSection`
    - `DexcomStatusCard`
    - `UnlinkDialog` (modal)
  - `DisplaySettingsSection`
  - `SystemInfoSection`
- `DexcomCallbackPage` (route-only page with spinner; outside of the Settings layout)
- Shared utilities: `ToastService`, `ConfirmDialogService`, `SettingsApi` (service), `DexcomApi` (service)

## 4. Component Details
### SettingsView
- Component description: Shell for all settings sections; renders section nav and content; syncs section with route; coordinates initial parallel loads.
- Main elements: header; grid layout with side nav and content; router outlet or conditional section containers.
- Handled interactions: section navigation; route/fragment sync; responsive layout.
- Handled validation: none.
- Types: `SettingsRouteKey`, `LoadState`.
- Props: none.

### SettingsSectionNav
- Component description: Accessible nav (tabs or vertical list) to jump to sections with keyboard support.
- Main elements: `<nav>` with list of links/buttons.
- Handled interactions: click/Enter/Space to navigate; Arrow keys for focus.
- Handled validation: none.
- Types: `SettingsRouteKey`.
- Props: `active: SettingsRouteKey`, `onNavigate: (key: SettingsRouteKey) => void`.

### AccountPreferencesSection
- Component description: Shows and edits TIR bounds with immediate preview; Save/Cancel.
- Main elements: heading; helper copy; `TirRangeForm`; preview chip for current range.
- Handled interactions: edit lower/upper; Save; Cancel.
- Handled validation:
  - `0 ≤ lower < upper ≤ 1000` mg/dL (matches API)
  - Visual guardrail recommended range [50, 350] while inputs still allow 0..1000
- Types: `TirPreferencesDto`, `UpdatePreferencesRequestDto`, `AccountPreferencesVM`, `ValidationErrors`.
- Props: `initial: TirPreferencesDto`, `onSave: (req: UpdatePreferencesRequestDto) => Promise<void>`.

### TirRangeForm
- Component description: Numeric inputs and optional dual-range slider with inline errors and helper text.
- Main elements: two number inputs (mg/dL); optional slider (0..1000); Save/Cancel buttons; error list.
- Handled interactions: input change; blur; submit; cancel.
- Handled validation: each in [0,1000]; cross `lower < upper`; disable Save when invalid.
- Types: `AccountPreferencesVM`, `ValidationErrors`.
- Props: `value`, `onChange`, `onSubmit`, `onCancel`.

### DataSourcesSection
- Component description: Shows Dexcom link status and provides Link/Unlink actions.
- Main elements: `DexcomStatusCard`; Link button; Unlink button; Refresh button.
- Handled interactions: Link (redirect to `/api/dexcom/authorize`); Unlink (open `UnlinkDialog`); Refresh status.
- Handled validation: enablement based on `status.isLinked` and loading flags.
- Types: `DexcomStatusDto`.
- Props: `status: DexcomStatusDto | null`, `onUnlink`, `onRefresh`.

### DexcomStatusCard
- Component description: Displays link state, linked at, token expiry, last sync; status badge.
- Main elements: badge; definition list; small print guidance.
- Handled interactions: none; action buttons live in parent.
- Types: `DexcomStatusDto`.
- Props: `status`.

### UnlinkDialog
- Component description: Confirmation modal to unlink Dexcom; explains retention; no purge toggle in MVP.
- Main elements: title; explanatory text; Confirm/Cancel.
- Handled interactions: Confirm → call parent; Cancel → close.
- Types: none.
- Props: `open`, `onConfirm`, `onCancel`.

### DexcomCallbackPage
- Component description: Captures `code` from query, calls POST `/api/dexcom/link`, shows progress and redirects to Data Sources.
- Main elements: spinner; success/error state; timed redirect notice.
- Handled interactions: automatic; link to retry OAuth on failure.
- Handled validation: missing `code` → error.
- Types: `LinkDexcomRequestDto`, `LinkDexcomResponseDto`.
- Props: none.

### DisplaySettingsSection
- Component description: Read-only note (theme fixed to dark); link to Account for TIR.
- Main elements: info card.
- Handled interactions: none.
- Types: none.
- Props: none.

### SystemInfoSection
- Component description: Read-only environment info (app version, environment), link to `/health`.
- Main elements: cards; external link.
- Handled interactions: open health link.
- Types: `SystemInfo`.
- Props: none.

## 5. Types
```typescript
// DTOs (backend contracts)
export interface TirPreferencesDto { // GET /api/account/preferences
  tirLowerBound: number;
  tirUpperBound: number;
}

export interface UpdatePreferencesRequestDto { // PUT /api/account/preferences
  tirLowerBound: number;
  tirUpperBound: number;
}

export interface DexcomStatusDto { // GET /api/dexcom/status
  isLinked: boolean;
  linkedAt: string | null;
  tokenExpiresAt: string | null;
  lastSyncAt: string | null;
}

export interface LinkDexcomRequestDto { // POST /api/dexcom/link
  authorizationCode: string;
}

export interface LinkDexcomResponseDto {
  linkId: string;
  linkedAt: string;
  tokenExpiresAt: string;
}

// View models
export interface AccountPreferencesVM {
  lower: number;
  upper: number;
  initialLower: number;
  initialUpper: number;
  isDirty: boolean;
  isValid: boolean;
  errors: ValidationErrors;
  saving: boolean;
}

export type ValidationErrors = {
  lower?: string;
  upper?: string;
  cross?: string;
};

export interface DexcomLinkVM {
  status: DexcomStatusDto | null;
  loading: boolean;
  linking: boolean;
  unlinking: boolean;
  error?: string;
}

export type SettingsRouteKey = 'account' | 'data-sources' | 'display' | 'system';

export interface SystemInfo {
  appVersion: string;
  environment: string;
  healthUrl: string;
}
```

## 6. State Management
- Use Angular signals for local state per section.
  - `AccountPreferencesStore`: signal of `AccountPreferencesVM`; methods: `load()`, `edit(partial)`, `validate()`, `save()`, `reset()`.
  - `DexcomStore`: signal of `DexcomLinkVM`; methods: `loadStatus()`, `unlink()`, `setError()`.
- Global toasts via `ToastService` (`aria-live` region in layout).
- Navigation: derive `activeSection` from route; keep URL in sync when user navigates via nav.

## 7. API Integration
- Include `withCredentials: true` for cookie-auth endpoints.
- Requests:
  - GET `/api/account/preferences` → `TirPreferencesDto`
  - PUT `/api/account/preferences` body `UpdatePreferencesRequestDto` → 200 OK
  - GET `/api/dexcom/status` → `DexcomStatusDto`
  - POST `/api/dexcom/link` body `LinkDexcomRequestDto` → `LinkDexcomResponseDto` (201)
  - DELETE `/api/dexcom/unlink` → 200 OK
- Example service methods (Angular `HttpClient`):
```typescript
getPreferences() { return this.http.get<TirPreferencesDto>('/api/account/preferences', { withCredentials: true }); }
updatePreferences(req: UpdatePreferencesRequestDto) { return this.http.put('/api/account/preferences', req, { withCredentials: true }); }
getDexcomStatus() { return this.http.get<DexcomStatusDto>('/api/dexcom/status', { withCredentials: true }); }
linkDexcom(code: string) { return this.http.post<LinkDexcomResponseDto>('/api/dexcom/link', { authorizationCode: code }, { withCredentials: true }); }
unlinkDexcom() { return this.http.delete('/api/dexcom/unlink', { withCredentials: true }); }
```
- OAuth start: `window.location.href = '/api/dexcom/authorize'`.
- Callback: read `code`; call `linkDexcom`; on success, redirect to `/settings/data-sources` with toast.

## 8. User Interactions
- Section navigation: keyboard/ARIA-compliant; updates focus and URL.
- TIR edit: inline validation; Save/Cancel; success toast; focus returns to heading.
- Link Dexcom: redirect to OAuth; upon return, auto-link and redirect back.
- Unlink Dexcom: confirm dialog; on success refresh status; info toast.
- Refresh: manual reload of status when needed.

## 9. Conditions and Validation
- TIR bounds: integers in [0,1000]; `lower < upper`; disable Save until valid and dirty.
- Dexcom buttons: Link disabled when already linked; Unlink disabled when not linked or busy.
- Callback requires `code`; missing `code` shows error with retry link.
- Auth: 401 → navigate to Login.

## 10. Error Handling
- Map to PRD buckets:
  - 401 Reconnect → route to Login and show toast.
  - 429 Slow down → toast and backoff (for status polling/refresh only).
  - Network issues → inline error with retry buttons.
  - Parsing issues → generic toast; log details to console.
- Specifics:
  - Preferences 400 → surface field messages; keep inputs.
  - Link 400 → show actionable retry message.
  - Unlink 404 → treat as already unlinked; info toast; refresh.
- Accessibility: `aria-live` announcements; focus to error summary.

## 11. Implementation Steps
1. Routes: add `/settings` and children; add `/dexcom-link` route.
2. Scaffolding: create components and styles with Tailwind and Angular standalone components.
3. Services: implement `SettingsApi`, `DexcomApi` with HttpClient.
4. Stores: implement signal-based stores and wire to components.
5. Account: build `TirRangeForm`, validation, Save/Cancel flows.
6. Data Sources: status card; Link/Unlink flows; dialog.
7. Callback: parse `code`, call link API, redirect on success.
8. Display/System: render read-only info and `/health` link.
9. A11y and toasts: add `aria-live` region, focus management.
10. Tests: unit tests for validation and stores; minimal e2e for link/unlink and TIR update.
