# View Implementation Plan Auth: Login

## 1. Overview
Authenticate existing users and start a secure session using httpOnly cookies set by the backend. The `/login` view presents an accessible email/password form with inline errors, disables submit while pending, optionally shows a session-expired banner when redirected by an auth guard, and provides a link to registration.

## 2. View Routing
- Path: `/login`
- Guarding: Public route. If already authenticated (client-side check via a lightweight session ping or existing app auth state), redirect to `/dashboard`.
- Query params: `?reason=sessionExpired` displayed as a banner; optional `?redirect=/path` to navigate after successful login.

## 3. Component Structure
- `LoginPageComponent` (standalone)
  - `SessionExpiredBannerComponent` (conditional)
  - `LoginFormComponent`
  - `AuthFooterLinksComponent`

## 4. Component Details
### LoginPageComponent
- Component description: Page shell for the login experience. Hosts banner, the actual form, and footer links.
- Main elements: page heading, optional `SessionExpiredBannerComponent`, `LoginFormComponent`, `AuthFooterLinksComponent`.
- Handled interactions:
  - Receives `loginSuccess` event from `LoginFormComponent` and navigates to redirect target (default `/dashboard`).
  - Reads `ActivatedRoute` query params to show banner and determine redirect path.
- Handled validation: Delegated to `LoginFormComponent`.
- Types: `AuthLoginViewModel` (page-level flags), `LoginSuccessEvent`.
- Props: none.

### SessionExpiredBannerComponent
- Component description: Non-blocking inline banner shown when redirected after session timeout/401.
- Main elements: container with icon/text; `role="status"`; close button to dismiss.
- Handled interactions:
  - `dismiss` click to hide locally.
- Handled validation: none.
- Types: `SessionBannerProps`.
- Props:
  - `visible: boolean`
  - `message?: string` (default: "Your session expired. Please log in again.")

### LoginFormComponent
- Component description: Accessible email/password form using Angular Reactive Forms; manages submission lifecycle and inline errors.
- Main elements: `<form>` with email `<input type="email">`, password `<input type="password">`, submit `<button>`; error text tied via `aria-describedby`.
- Handled interactions:
  - `submit` → calls `AuthService.login()`; emits `loginSuccess` with `LoginResponseDto` on 200.
  - Field validation on blur/input; keyboard-first (Enter submits when valid and not pending).
- Handled validation:
  - Email: required, valid email format.
  - Password: required. Do not enforce 12+ here (policy enforced on registration). Allow server to reject invalid credentials.
  - Disable submit when form invalid or `isSubmitting`.
  - Show server errors inline for email/password as appropriate; show account lockout vs generic auth failure distinctly.
- Types: `LoginFormValue`, `LoginFormState`, `LoginRequestDto`, `LoginResponseDto`, `ProblemDetails`.
- Props:
  - `autofocusEmail?: boolean` (default true)
  - Output: `loginSuccess: EventEmitter<LoginResponseDto>`

### AuthFooterLinksComponent
- Component description: Footer links relevant to authentication flows.
- Main elements: link to `/register`. No “forgot password” in MVP per PRD.
- Handled interactions: navigation link activation.
- Handled validation: none.
- Types: none
- Props: none

## 5. Types
- DTOs (mirror backend):
  - `LoginRequestDto`:
    - `email: string`
    - `password: string`
  - `LoginResponseDto` (maps to API `LoginResponse`):
    - `userId: string` (GUID)
    - `email: string`
  - `ProblemDetails` (ASP.NET Core):
    - `type?: string`
    - `title?: string`
    - `status?: number`
    - `detail?: string`
    - `instance?: string`
    - `[key: string]: unknown` (extensions)

- View models:
  - `LoginFormValue`:
    - `email: string`
    - `password: string`
  - `LoginFormState`:
    - `isSubmitting: boolean`
    - `serverError?: string` (general error to show above form)
    - `fieldErrors?: { email?: string; password?: string }`
  - `AuthLoginViewModel`:
    - `showSessionExpired: boolean`
    - `redirectTo?: string` (defaults to `/dashboard`)
  - `LoginSuccessEvent`:
    - `user: LoginResponseDto`
    - `redirectTo: string`
  - `SessionBannerProps`:
    - `visible: boolean`
    - `message?: string`

## 6. State Management
- Use Angular Reactive Forms in `LoginFormComponent` with a `FormGroup<LoginFormValue>`.
- Local component state only:
  - `isSubmitting` to control button disabled/spinner
  - `serverError` and `fieldErrors` for inline feedback
- Page-level derived state in `LoginPageComponent` from `ActivatedRoute` query params to set `showSessionExpired` and `redirectTo`.
- No global auth token stored in JS. Rely on httpOnly cookies set by backend; only store non-sensitive `LoginResponseDto` in an app-level user store if needed later (optional, not required for this view).

## 7. API Integration
- Endpoints used:
  - `POST /api/auth/login`
- Request:
  - Body: `LoginRequestDto`
  - Headers: `Content-Type: application/json`
  - Credentials: `withCredentials: true` to allow backend to set `access_token` and `refresh_token` cookies (httpOnly, SameSite=Lax, Secure).
- Response (200): `LoginResponseDto` with `{ userId, email }`. Tokens are NOT present in body.
- Error responses:
  - 401 Unauthorized with `ProblemDetails` (titles include "Authentication Failed" or "Account Locked").
  - 400 Bad Request with `ProblemDetails` possible for invalid payload (rare in well-validated UI).
  - Network/CORS failures.
- Angular specifics:
  - Ensure CORS is configured server-side to allow credentials and the app origin; client must pass `withCredentials: true`.
  - Optional: Enable Angular XSRF module for state-changing calls elsewhere; not required for reading the login response.

## 8. User Interactions
- Enter key on password field submits when valid and not submitting.
- Submit button is disabled while pending or when form invalid.
- Inline validation appears after blur or submit attempt (whichever comes first).
- On 200, emit `loginSuccess`, store `LoginResponseDto` if app requires, navigate to `redirectTo` or `/dashboard`.
- On 401 with title "Account Locked": show distinct lockout message and keep focus on the heading; prevent repeated submissions for a brief debounce window.
- On 401 generic: show "Invalid email or password" and keep the email value; focus the error summary.
- On network error: show actionable message "Cannot reach server. Check connection and try again." Retry enabled; submit remains enabled.
- Session-expired banner shown when `reason=sessionExpired` is present; dismissible.

## 9. Conditions and Validation
- Email field:
  - Required; RFC 5322-basic email pattern via Angular `Validators.email`.
  - `aria-invalid` reflects invalid state; `aria-describedby` points to inline error element.
- Password field:
  - Required. No client-side minimum length enforcement for login (policy enforced on registration per PRD). Do not trim.
- Form-level:
  - Disable submit when invalid or `isSubmitting`.
  - Prevent duplicate submissions by ignoring while pending.
- API conditions:
  - Must send JSON with required properties.
  - Must send `withCredentials: true` for cookies to be set. Verify by observing 200 status and proceeding without reading cookies.

## 10. Error Handling
- 401 Unauthorized (Authentication Failed):
  - Display inline message mapped from `ProblemDetails.detail` or a safe generic fallback.
  - Clear password input only; keep email.
- 401 Unauthorized (Account Locked):
  - Display lockout-specific message and prevent rapid retries (debounce UI for a few seconds).
- 400 Bad Request:
  - Map to general error; if `ProblemDetails` includes field hints, surface beside fields.
- Network/CORS:
  - Show connection issue; suggest retry. Log client-side error with correlation id if present in headers.
- Accessibility:
  - Focus management: On error, move focus to error summary or first invalid field.
  - Announce errors via `role="alert"`/`aria-live="assertive"`.

## 11. Implementation Steps
1. Create `AuthService` with `login(payload: LoginRequestDto): Observable<LoginResponseDto>` using `HttpClient.post` to `/api/auth/login` with `withCredentials: true`.
2. Create `SessionExpiredBannerComponent` (standalone) with `@Input() visible` and optional `@Input() message`; expose a local `dismiss()`.
3. Create `LoginFormComponent`:
   - Build Reactive Form with controls `email`, `password` and validators.
   - Render accessible labels, error messages tied via `aria-describedby`.
   - Handle submit: set `isSubmitting`, call `AuthService.login`, map success/error, manage `serverError`/`fieldErrors`, and emit `loginSuccess` on success.
   - Disable submit while pending; show spinner state.
4. Create `AuthFooterLinksComponent` with a link to `/register` and subtle copy regarding password policy on registration.
5. Create `LoginPageComponent`:
   - Read `ActivatedRoute` query params to compute `showSessionExpired` and `redirectTo`.
   - Handle `loginSuccess` by navigating to `redirectTo ?? '/dashboard'`.
6. Add route in the app router: `{ path: 'login', loadComponent: () => import('./.../login-page.component').then(m => m.LoginPageComponent) }`.
7. Styling: Apply Tailwind for dark mode; ensure sufficient contrast and large tap targets. Keep layout desktop-first.
8. Accessibility: Ensure labels, descriptions, focus ring visibility, and keyboard order. Add `aria-live` regions for errors and banner.
9. Testing:
   - Unit: form validation, disabled button state, success path (mock service), 401 mapping, lockout handling, network error handling.
   - E2E happy path (optional here; covered by global e2e): login redirects to dashboard on success.
10. Integration checks:
   - Verify cookies set in browser devtools (cannot be read via JS). Ensure CORS allows credentials in the environment.
   - Confirm redirect behavior respects `?redirect=` parameter when provided.


