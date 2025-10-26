# View Implementation Plan – Add Event Modal

## 1. Overview
The Add Event modal enables users to create Food, Insulin, Exercise, or Note events directly from the Dashboard without navigating away. It provides a dropdown event type selector that swaps in type-specific fields, strict validation aligned with backend DTOs, inline error mapping from ProblemDetails, keyboard/accessibility support, and posts successful creations to the chart overlay and History without recentering.

## 2. View Routing
- Invoked from `/dashboard` via an "Add Event" button or keyboard shortcut.
- Not a standalone route; rendered as a modal overlay within the Dashboard view.
- Optional enhancement (later): auxiliary router outlet `(modal:add-event)` for deep-linking.

## 3. Component Structure
- `DashboardPage`
  - `AddEventModalComponent`
    - `EventTypeSelectComponent`
    - `FoodEventFormComponent`
    - `InsulinEventFormComponent`
    - `ExerciseEventFormComponent`
    - `NoteEventFormComponent`
    - `ProblemDetailsAlertComponent`
    - `ModalFooter` (Cancel, Submit)

## 4. Component Details
### AddEventModalComponent
- Component description: Container dialog that manages focus trap, selected event type, form instances, submission, inline error mapping, and success callbacks.
- Main elements: dialog container with `role="dialog"` and `aria-modal="true"`, header (title, close button, event type dropdown), content area (selected form), footer with Cancel/Submit.
- Handled interactions:
  - Open/close; Escape to close; click overlay to close (with confirmation if dirty).
  - Change event type via dropdown (Food default). Preserve per-type form state while modal remains open.
  - Submit disabled while pending; Enter submits when focused inside form.
- Handled validation:
  - Common: `eventTime` required and must not be in the future.
  - Per-type: see form components below.
  - Client-enforced numeric bounds; server-side ProblemDetails mapped inline.
- Types: uses `AddEventType`, `ProblemDetails`, form ViewModels.
- Props (Inputs/Outputs):
  - Inputs: `isOpen: boolean`
  - Outputs: `closed: void`, `created: EventResponseUnion` (emits created event for chart/history update)

### EventTypeSelectComponent
- Component description: Renders a single dropdown to select the event type: Food (default), Insulin, Exercise, Note.
- Main elements: label + native `<select>` (or headless listbox) with ARIA `aria-label`/`aria-labelledby`.
- Handled interactions: click/keyboard to change selected type; emits selection.
- Handled validation: required (must have a value; defaults to Food).
- Types: `AddEventType`, `LookupOption`.
- Props: `selected: AddEventType`, `options: LookupOption[]`, `selectionChange: AddEventType` (Output)

### FoodEventFormComponent
- Component description: Reactive form for Food event creation.
- Main elements: inputs for `eventTime` (datetime-local), `carbohydratesGrams` (number), `mealTagId` (select), `absorptionHint` (select), `note` (textarea).
- Handled interactions: input change; blur validation; displays field-level errors; exposes `FormGroup`.
- Handled validation:
  - `eventTime`: required, not in future.
  - `carbohydratesGrams`: required, integer 0–300 inclusive.
  - `mealTagId`: optional.
  - `absorptionHint`: optional, constrained to `Fast|Medium|Slow`.
  - `note`: optional, max length 500.
- Types: `FoodEventFormModel`, `CreateFoodEventRequest`, `LookupOption` for meal tags, `FoodAbsorptionHint`.
- Props: `form: FormGroup<FoodEventFormModel>`, `lookups: { mealTags: LookupOption[] }`, `problem: FieldProblemMap | null`.

### InsulinEventFormComponent
- Component description: Reactive form for Insulin event creation.
- Main elements: inputs for `eventTime` (datetime-local), `insulinType` (segmented control or radio), `insulinUnits` (number step 0.5), `preparation`, `delivery`, `timing` (text), `note` (textarea).
- Handled interactions: same as above.
- Handled validation:
  - `eventTime`: required, not in future.
  - `insulinType`: required, one of `Fast|Long`.
  - `insulinUnits`: required, number 0–100 inclusive, step 0.5 (enforced by custom validator for half-step multiples).
  - `preparation`, `delivery`, `timing`: optional, max length 200.
  - `note`: optional, max length 500.
- Types: `InsulinEventFormModel`, `CreateInsulinEventRequest`, `InsulinType` union.
- Props: `form: FormGroup<InsulinEventFormModel>`, `problem: FieldProblemMap | null`.

### ExerciseEventFormComponent
- Component description: Reactive form for Exercise event creation.
- Main elements: inputs for `eventTime` (datetime-local), `exerciseTypeId` (select), `durationMinutes` (number), `intensity` (select), `note` (textarea).
- Handled validation:
  - `eventTime`: required, not in future.
  - `exerciseTypeId`: required (select from known list).
  - `durationMinutes`: required, integer 1–300 inclusive.
  - `intensity`: optional, `Low|Moderate|High`.
  - `note`: optional, max length 500.
- Types: `ExerciseEventFormModel`, `CreateExerciseEventRequest`.
- Props: `form: FormGroup<ExerciseEventFormModel>`, `lookups: { exerciseTypes: LookupOption[], intensities: LookupOption[] }`, `problem: FieldProblemMap | null`.

### NoteEventFormComponent
- Component description: Reactive form for free-text Note event.
- Main elements: inputs for `eventTime` (datetime-local), `noteText` (textarea with live char count).
- Handled validation:
  - `eventTime`: required, not in future.
  - `noteText`: required, length 1–500.
- Types: `NoteEventFormModel`, `CreateNoteEventRequest`.
- Props: `form: FormGroup<NoteEventFormModel>`, `problem: FieldProblemMap | null`.

### ProblemDetailsAlertComponent
- Component description: Displays API ProblemDetails title/detail and field error list; supports mapping to specific fields.
- Main elements: alert region (`role=alert`), list of errors, optional "Try again" guidance.
- Handled interactions: none.
- Types: `ProblemDetails`, `FieldProblemMap`.
- Props: `problem: ProblemDetails | null`, `fieldMap?: Record<string,string>` to map backend field names to form control keys.

## 5. Types
- Request DTOs (TypeScript):
```typescript
export type IsoDateTimeString = string; // ISO 8601 with offset (e.g., 2025-10-26T14:05:00-04:00)

export interface CreateFoodEventRequest {
  eventTime: IsoDateTimeString;
  carbohydratesGrams: number;
  mealTagId?: number | null;
  absorptionHint?: FoodAbsorptionHint | null; // "Fast" | "Medium" | "Slow"
  note?: string | null; // <= 500
}

export interface CreateInsulinEventRequest {
  eventTime: IsoDateTimeString;
  insulinType: InsulinType; // "Fast" | "Long"
  insulinUnits: number; // 0–100 in 0.5 steps
  preparation?: string | null; // <= 200
  delivery?: string | null; // <= 200
  timing?: string | null; // <= 200
  note?: string | null; // <= 500
}

export interface CreateExerciseEventRequest {
  eventTime: IsoDateTimeString;
  exerciseTypeId: number; // required
  durationMinutes: number; // 1–300
  intensity?: ExerciseIntensity | null; // "Low" | "Moderate" | "High"
  note?: string | null; // <= 500
}

export interface CreateNoteEventRequest {
  eventTime: IsoDateTimeString;
  noteText: string; // 1–500
}

export type FoodAbsorptionHint = 'Fast' | 'Medium' | 'Slow';
export type InsulinType = 'Fast' | 'Long';
export type ExerciseIntensity = 'Low' | 'Moderate' | 'High';

export interface EventResponseBase {
  eventId: string;
  eventType: 'Food' | 'Insulin' | 'Exercise' | 'Note';
  eventTime: IsoDateTimeString;
  createdAt: IsoDateTimeString;
  note?: string | null;
}

export interface FoodEventResponse extends EventResponseBase {
  carbohydratesGrams: number;
  mealTagId?: number | null;
  absorptionHint?: FoodAbsorptionHint | null;
}

export interface InsulinEventResponse extends EventResponseBase {
  insulinType: InsulinType;
  insulinUnits: number;
  preparation?: string | null;
  delivery?: string | null;
  timing?: string | null;
}

export interface ExerciseEventResponse extends EventResponseBase {
  exerciseTypeId: number;
  durationMinutes: number;
  intensity?: ExerciseIntensity | null;
}

export interface NoteEventResponse extends EventResponseBase {
  // Base.note contains the text
}

export type EventResponseUnion =
  | FoodEventResponse
  | InsulinEventResponse
  | ExerciseEventResponse
  | NoteEventResponse;

export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  traceId?: string;
  errors?: Record<string, string[]> | null;
}

export interface LookupOption {
  value: number | string;
  label: string;
}

// ViewModels for reactive forms (datetime-local strings kept local, converted on submit)
export interface FoodEventFormModel {
  eventTimeLocal: string; // yyyy-MM-ddTHH:mm
  carbohydratesGrams: number | null;
  mealTagId?: number | null;
  absorptionHint?: FoodAbsorptionHint | null;
  note?: string | null;
}

export interface InsulinEventFormModel {
  eventTimeLocal: string;
  insulinType: InsulinType | null;
  insulinUnits: number | null;
  preparation?: string | null;
  delivery?: string | null;
  timing?: string | null;
  note?: string | null;
}

export interface ExerciseEventFormModel {
  eventTimeLocal: string;
  exerciseTypeId: number | null;
  durationMinutes: number | null;
  intensity?: ExerciseIntensity | null;
  note?: string | null;
}

export interface NoteEventFormModel {
  eventTimeLocal: string;
  noteText: string | null;
}

export type AddEventType = 'Food' | 'Insulin' | 'Exercise' | 'Note';

// Field-specific mapping from backend keys to form controls (optional)
export type FieldProblemMap = Record<string, string[]>;
```

## 6. State Management
- Local component state (no global store required):
  - `selectedType: AddEventType` (default 'Food').
  - `forms`: map of type -> `FormGroup` built with Angular Reactive Forms.
  - `isSubmitting: boolean` toggled during POST.
  - `problem: ProblemDetails | null` stored per submission; map `errors` to controls.
- Keep form state per selected type while modal open; reset on close.
- Parent updates: on success, emit `created(event)` to Dashboard, which updates chart overlays and History cache without recentering.
- Optional service: `ChartEventsService` to broadcast newly created event to any subscriber (chart, history panel).

## 7. API Integration
- Base URL: `/api/events` (case-insensitive on server; controller route is `api/Events`).
- Endpoints and statuses:
  - POST `/food` → 201 Created (FoodEventResponse) | 400 ProblemDetails | 401 ProblemDetails
  - POST `/insulin` → 201 Created (InsulinEventResponse) | 400 | 401
  - POST `/exercise` → 201 Created (ExerciseEventResponse) | 400 | 401
  - POST `/note` → 201 Created (NoteEventResponse) | 400 | 401
- Security: JWT via httpOnly cookie; CSRF: enable Angular `HttpClientXsrfModule` (cookie `XSRF-TOKEN`, header `X-XSRF-TOKEN`).
- Service API (example):
```typescript
@Injectable({ providedIn: 'root' })
export class EventsApiService {
  constructor(private http: HttpClient) {}

  createFood(body: CreateFoodEventRequest) {
    return this.http.post<FoodEventResponse>('/api/events/food', body);
  }
  createInsulin(body: CreateInsulinEventRequest) {
    return this.http.post<InsulinEventResponse>('/api/events/insulin', body);
  }
  createExercise(body: CreateExerciseEventRequest) {
    return this.http.post<ExerciseEventResponse>('/api/events/exercise', body);
  }
  createNote(body: CreateNoteEventRequest) {
    return this.http.post<NoteEventResponse>('/api/events/note', body);
  }
}
```
- Date conversion helpers:
```typescript
export function localDateTimeToOffsetIso(local: string): IsoDateTimeString {
  // local like '2025-10-26T14:05'
  const [date, time] = local.split('T');
  const [y, m, d] = date.split('-').map(Number);
  const [hh, mm] = time.split(':').map(Number);
  const dt = new Date(y, m - 1, d, hh, mm, 0, 0); // local time
  const tzOffsetMin = -dt.getTimezoneOffset(); // e.g., -(-240) = 240 for -04:00
  const sign = tzOffsetMin >= 0 ? '+' : '-';
  const abs = Math.abs(tzOffsetMin);
  const offH = String(Math.trunc(abs / 60)).padStart(2, '0');
  const offM = String(abs % 60).padStart(2, '0');
  const yyyy = String(dt.getFullYear());
  const MM = String(dt.getMonth() + 1).padStart(2, '0');
  const DD = String(dt.getDate()).padStart(2, '0');
  const HH = String(dt.getHours()).padStart(2, '0');
  const MMm = String(dt.getMinutes()).padStart(2, '0');
  const SS = '00';
  return `${yyyy}-${MM}-${DD}T${HH}:${MMm}:${SS}${sign}${offH}:${offM}`;
}
```
- Validators:
```typescript
export const notFutureDateTime: ValidatorFn = (control) => {
  const v = control.value as string | null;
  if (!v) return null;
  const asIso = localDateTimeToOffsetIso(v);
  const nowIso = localDateTimeToOffsetIso(formatNowLocal());
  return new Date(asIso) <= new Date(nowIso) ? null : { future: true };
};

export const halfStep = (control: AbstractControl) => {
  const v = control.value;
  if (v === null || v === undefined || v === '') return null;
  return Math.round(v * 2) === v * 2 ? null : { stepHalf: true };
};

function formatNowLocal(): string {
  const d = new Date();
  const yyyy = d.getFullYear();
  const MM = String(d.getMonth() + 1).padStart(2, '0');
  const DD = String(d.getDate()).padStart(2, '0');
  const HH = String(d.getHours()).padStart(2, '0');
  const mm = String(d.getMinutes()).padStart(2, '0');
  return `${yyyy}-${MM}-${DD}T${HH}:${mm}`;
}
```

## 8. User Interactions
- Open modal: focus moves to modal title; background content inert; focus trapped.
- Change event type via dropdown: form content switches; previously entered values preserved per type while open.
- Submit:
  - While pending: disable inputs and buttons; show spinner; prevent duplicate posts.
  - On success: close modal; show success toast; emit created event to parent; chart updates with new marker; do not recenter.
  - On 400: show ProblemDetails alert; map `errors` to fields; focus first invalid field.
  - On 401: redirect to login page.
  - On network error: show retry guidance; keep form data.
- Cancel: if dirty, confirm; otherwise close immediately.
- Keyboard: Escape closes; Enter submits; Tab cycles within modal.

## 9. Conditions and Validation
- Food:
  - `carbohydratesGrams` required; integer 0–300.
  - `absorptionHint` constrained to `Fast|Medium|Slow` (optional).
  - `note` <= 500.
- Insulin:
  - `insulinType` required: `Fast|Long`.
  - `insulinUnits` required: 0–100, step 0.5 enforced by validator and input `step="0.5"`.
  - `preparation|delivery|timing` ≤ 200 chars.
  - `note` ≤ 500.
- Exercise:
  - `exerciseTypeId` required.
  - `durationMinutes` required: 1–300 integer.
  - `intensity` optional: `Low|Moderate|High`.
  - `note` ≤ 500.
- Note:
  - `noteText` required 1–500 chars.
- All:
  - `eventTime` required and must not be future. Convert `datetime-local` to offset ISO before POST.

## 10. Error Handling
- 400 ProblemDetails: render `title` + `detail`; display `errors[field]` messages beneath corresponding controls. Unknown fields appear in the alert list.
- 401 ProblemDetails: route to `/auth/login`; preserve an intent state to reopen modal after login if desired.
- Network errors/timeouts: show toast with Retry; keep form state.
- Server-side domain errors (e.g., "future time"): show as inline and focus field.
- Accessibility: alert region is announced on error; on success, toast is announced via `aria-live=polite`.

## 11. Implementation Steps
1. Create `EventsApiService` with four POST methods and response typings.
2. Enable CSRF: add Angular `HttpClientXsrfModule.withOptions({ cookieName: 'XSRF-TOKEN', headerName: 'X-XSRF-TOKEN' })` in app module.
3. Implement shared utilities: `localDateTimeToOffsetIso`, validators (`notFutureDateTime`, `halfStep`), `formatNowLocal`.
4. Implement `ProblemDetailsAlertComponent` for displaying API errors.
5. Implement `EventTypeSelectComponent` with accessible label and keyboard interactions.
6. Implement form components (`FoodEventFormComponent`, `InsulinEventFormComponent`, `ExerciseEventFormComponent`, `NoteEventFormComponent`) using Reactive Forms, validators, and lookups.
7. Implement `AddEventModalComponent`:
   - Build and keep form instances per type; default selected type to `Food`; default each form time to `formatNowLocal()`.
   - Trap focus; handle Escape; disable submit while `isSubmitting`.
   - On submit, map form models to DTOs (convert `eventTimeLocal` to offset ISO; sanitize strings; coerce numbers).
   - Call service; handle statuses; on success emit `created(event)` and close; on 400 apply `errors` to controls and display `ProblemDetailsAlertComponent`.
8. Integrate into Dashboard: Add "Add Event" button; conditionally render modal; subscribe to `created` and push to chart overlays and History list without recentering.
9. Add basic Tailwind styles consistent with dark mode.
10. Add unit tests for validators and mapping helpers; component tests for form validation and submission; e2e happy path aligned with PRD (login → add each event type).

---
- Lookups (MVP): provide static constants for `eventTypes`, `mealTags`, `exerciseTypes`, `intensities` aligned with domain, with an `Other` where available; store extra descriptive text in `note` if needed. Replace with backend-provided lists when available.
