## View Implementation Plan — Auth: Register

### 1. Overview
 Build a dedicated registration view at `/register` that lets a new user create an account by entering a unique email and a password meeting the minimum 12-character requirement. On success, prompt the user to log in (and optionally redirect to `/login`). The page must show inline validation, announce errors via aria-live, and avoid autofill pitfalls. All validation is handled via form validators.

### 2. View Routing
- Path: `/register`
- Route entry: add a lazy or direct route in `src/app/app.routes.ts`
  - Data: `{ title: 'Register' }` to support document title

```ts
// app.routes.ts (excerpt)
export const routes: Routes = [
  { path: 'register', loadComponent: () => import('./features/auth/register/register-page.component').then(m => m.RegisterPageComponent), data: { title: 'Register' } },
  // ... other routes
];
```

### 3. Component Structure
- RegisterPageComponent (standalone container)
  - Renders header, `RegisterFormComponent`, optional success callout
  - Orchestrates API call, navigation to `/login`
- RegisterFormComponent (standalone presentational)
  - Reactive form for `email` and `password`
  - Inline validation and aria-live region
  - Emits submit event with form values

### 4. Component Details
#### RegisterPageComponent
- Description: Container for the register view. Wires `AuthApiService.register(...)`, handles loading/error/success, and navigates to `/login` upon success.
- Main elements:
  - Page container with heading “Create your account”
  - `RegisterFormComponent`
  - Optional success banner/dialog with “Go to Login” button
- Handled interactions:
  - Submit event from child → call API
  - Click “Go to Login” → navigate to `/login?registered=true`
- Handled validation:
  - Delegated to child form; shows server-level errors if provided (e.g., 409 email exists)
- Types:
  - Uses `RegisterRequest`, `RegisterResponse`, `ProblemDetails`, `RegisterFormModel`
- Props: none (top-level route component)

#### RegisterFormComponent
- Description: Presentational reactive form collecting email and password with inline validation and accessibility aids.
- Main elements:
  - Email input (`type="email"`, `autocomplete="email"`)
  - Password input (`type="password"`, `autocomplete="new-password"`) + show/hide toggle
  - Submit button (disabled while invalid or submitting)
  - Aria-live region for errors (polite)
- Handled interactions:
  - Input change/blur → field-level validation
  - Submit → emit `(submitted)` with `RegisterFormModel`
- Handled validation (client-side):
  - Email: required, valid email format
  - Password: required, minLength 12; extra guard to prevent all-whitespace or repeated single char (soft warning)
  - Server errors: set as form errors, e.g., `{ emailTaken: true }` on email control
- Types:
  - `RegisterFormModel`
- Props:
  - `isSubmitting: boolean`
  - `serverError?: string` (form-level)
  - `emailTaken?: boolean` (optional convenience)
  - `submitted: EventEmitter<RegisterFormModel>`

#### PasswordStrengthHintComponent
- Description: Simple live feedback on password input; does not block submission beyond min length.
- Main elements:
  - Hint text with dynamic badge/state (weak/ok/strong)
- Handled interactions:
  - Input value change (via `@Input() password: string`)
- Handled validation:
  - Computes a non-blocking score; enforces only the minimum 12-character requirement visually
- Types:
  - none new (string input)
- Props:
  - `password: string`

### 5. Types
```ts
// Backend contracts (mirror shapes)
export interface RegisterRequest {
  email: string;
  password: string; // min 12 chars
}

export interface RegisterResponse {
  userId: string; // Guid
  email: string;
  registeredAt: string; // ISO Date
}

export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  [key: string]: unknown;
}

// View models
export interface RegisterFormModel {
  email: string;
  password: string;
}

export interface RegisterUiState {
  isSubmitting: boolean;
  serverError?: string; // top-level error (network/unknown)
  emailTaken?: boolean; // 409 specialization
  success?: boolean;
}
```

### 6. State Management
- Local component state (no global store):
  - `FormGroup` for form fields
  - `isSubmitting` boolean state to disable form and button
  - `serverError` string for non-field errors
  - `emailTaken` flag (to set control error and hint)
  - `success` flag to optionally show a success callout before navigating
- Optional small facade service (not required): Expose `register(formValue)` returning `Observable<RegisterResponse>` to keep the component lean.

### 7. API Integration
- Endpoint: `POST /api/auth/register`
- Request body (JSON): `RegisterRequest` with `{ email, password }`
- Success (201 Created): `RegisterResponse` body
- Error:
  - 400 BadRequest → `ProblemDetails` (validation or policy failure)
  - 409 Conflict → `ProblemDetails` with title “Email Already Exists”

```ts
// AuthApiService (excerpt)
@Injectable({ providedIn: 'root' })
export class AuthApiService {
  constructor(private readonly http: HttpClient) {}

  register(body: RegisterRequest): Observable<RegisterResponse> {
    return this.http.post<RegisterResponse>('/api/auth/register', body, {
      observe: 'body'
    });
  }
}
```

### 8. User Interactions
- Enter email/password → live validation updates
- Submit:
  - If form invalid → prevent submit, focus first invalid control, announce errors
  - If valid → call API; while pending disable inputs and show spinner on button
  - On 201 → set `success` and navigate to `/login?registered=true` (or show success card with CTA)
  - On 409 → set field error on email (`emailTaken`), focus email, describe inline
  - On 400 → show `problem.detail` under form; focus the form error region
  - On network/5xx → show actionable banner “Try again”

### 9. Conditions and Validation
- Email:
  - Required, valid format (`Validators.required`, `Validators.email`)
  - Server: uniqueness enforced; map 409 to `emailTaken`
- Password:
  - Required, `Validators.minLength(12)`; additional soft checks may be included directly in the validator (non-blocking warnings)
- Submit disabled conditions:
  - `form.invalid || isSubmitting`
- Autofill and security considerations:
  - `autocomplete="email"` for email
  - `autocomplete="new-password"` for password
  - Avoid storing any credentials in local storage
- Accessibility:
  - Each input associated with `label` and `aria-describedby`
  - Inline errors in `mat-error`/small text within an `aria-live="polite"` region
  - Move focus to first error on failed submit

### 10. Error Handling
- 409 Conflict (email exists): mark email control error, show helper text “Email is already registered. Try logging in.” and offer a link to `/login`.
- 400 Bad Request: display `problem.detail` in a form-level error box; if shape contains per-field info (future), map appropriately.
- 0/Network or 5xx: show retry banner; keep entered values; do not clear form.
- Generic fallback: “Registration failed. Please try again.”

### 11. Implementation Steps
1. Provide HttpClient in app config:
   - Update `app.config.ts` providers: `provideHttpClient()`
2. Add route entry for `/register` in `app.routes.ts` (lazy load or direct standalone component).
3. Create `AuthApiService` in `src/app/core/services/auth-api.service.ts` with `register()`.
4. Create `RegisterPageComponent` in `src/app/features/auth/register/register-page.component.ts`:
   - Standalone; imports: `CommonModule`, `ReactiveFormsModule`, Material form modules, `RegisterFormComponent`
   - Inject `AuthApiService`, `Router`
   - Handle submit: set `isSubmitting`; call API; on success navigate to `/login?registered=true`; handle errors and set form/server errors
5. Create `RegisterFormComponent` in same folder:
   - Standalone; inputs: `isSubmitting`, `serverError`, `emailTaken`
   - Output: `submitted`
   - Build `FormGroup` with validators; render inputs + errors; aria-live region
   - Include password visibility toggle.
6. Styling:
   - Use Angular Material + utility classes (Tailwind if available) for spacing and contrast
   - Ensure sufficient contrast in dark mode
7. Accessibility:
   - Add `aria-live` for errors; focus management on submit failure; labels and descriptions
8. QA and flows:
   - Valid submit → navigates to login with success flag
   - Short password → inline error blocks submit
   - Duplicate email → inline email error from 409
   - Offline → banner with retry
9. Optional: On the login page, read `registered=true` to show a success toast/banner “Account created. Please log in.”

### Appendix: Example Validators (excerpt)
```ts
const emailCtrl = new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.email] });
const passwordCtrl = new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.minLength(12)] });
const form = new FormGroup({ email: emailCtrl, password: passwordCtrl });

function setServerEmailTaken(control: AbstractControl, taken: boolean) {
  if (taken) control.setErrors({ ...(control.errors ?? {}), emailTaken: true });
  else if (control.hasError('emailTaken')) {
    const { emailTaken, ...rest } = control.errors ?? {}; control.setErrors(Object.keys(rest).length ? rest : null);
  }
}
```

### Appendix: Example Submit Handler (excerpt)
```ts
onSubmit(model: RegisterFormModel) {
  this.isSubmitting = true;
  this.serverError = undefined;
  this.emailTaken = false;

  this.authApi.register(model).pipe(finalize(() => (this.isSubmitting = false))).subscribe({
    next: () => this.router.navigate(['/login'], { queryParams: { registered: true } }),
    error: (err: HttpErrorResponse) => {
      const problem = err.error as ProblemDetails | undefined;
      if (err.status === 409) {
        this.emailTaken = true; // also set control error in form component
      } else if (problem?.detail) {
        this.serverError = problem.detail;
      } else {
        this.serverError = 'Registration failed. Please try again.';
      }
    }
  });
}
```
