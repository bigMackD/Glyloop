# View Implementation Plan – AppShell

## 1. Overview

The AppShell is the protected layout wrapper for `/dashboard` and `/settings/*`, delivering a consistent high-contrast dark experience with global navigation and access to user utilities (account menu). It enforces authenticated access, manages global focus flows, and frames the main content outlet without owning feature-specific data.

## 2. View Routing

- Path: Acts as the shared layout component applied to all protected routes; registered as the shell for `/dashboard`, `/settings`, and nested settings routes (default redirect to `/settings/data-sources`).
- Guarding: `AuthGuard` protects the shell routes; unauthenticated users are redirected to `/login`.
- Deep link support: Shell must gracefully host routes like `/settings/data-sources/dexcom/callback` for OAuth completion.
- Public routes (`/login`, `/register`) bypass the shell and use a separate minimal layout.

## 3. Component Structure

- `AppShellLayoutComponent`
  - `AppHeaderComponent`
    - `LogoCluster`
    - `NavTabsComponent`
    - `UserMenuComponent`
  - `<main class="app-shell__content" tabindex="-1">`
    - Angular `RouterOutlet`

All components are standalone with `ChangeDetectionStrategy.OnPush`, leverage Angular Material + Tailwind utility classes, and respect dark theme tokens.

## 4. Component Details

-### AppShellLayoutComponent
-
- Component description: Root layout wrapping protected routes; orchestrates authenticated navigation, renders header and router outlet, and provides context/services (signals) to descendants using existing frontend stores.
- Main elements: `<header>` containing `AppHeaderComponent`; `<main>` content area with `RouterOutlet`.
- Handled interactions: Listens to router navigation to set active tab.
- Handled validation:
  - Ensures authenticated status prior to rendering (via guard) and triggers logout redirect when auth state becomes invalid.
  - Verifies legal acknowledgement flag before showing shell (if missing, invokes the dedicated legal acknowledgement flow provided by the auth domain).
- Types: `ShellStateVM`, `ShellNavLink`.
- Props: Receives none (top-level route component); injects required services (`ShellStateService`, `Router`, `AuthFacade`).

-### AppHeaderComponent
-
- Component description: Top navigation bar containing brand, nav tabs, and user menu.
- Main elements: `<header>` with flex layout; `<nav>` for tabs; `<div>` for logo; `<div>` for user avatar button.
- Handled interactions: Tab click (navigates via router); keyboard navigation; user menu toggle; responsive collapse handling for small widths.
- Handled validation:
  - Active tab highlights when router url matches `ShellNavLink.path` (strict or prefix match for nested settings).
- Types: `ShellHeaderVM` (combines nav links and user summary), `ShellNavLink`, `ShellUserSummary`.
- Props: `viewModel: ShellHeaderVM`, `onNavigate: (link: ShellNavLink) => void`, `onLogout: () => void`.

### NavTabsComponent

- Component description: Renders top-level tabs for Dashboard and Settings.
- Main elements: Angular Material `mat-tab-nav-bar` or custom `<ul role="tablist">` with `<a role="tab">`.
- Handled interactions: Click/keyboard navigation; using routerLink; focus management when active route changes.
- Handled validation: Ensures correct ARIA attributes (`aria-selected`, `tabindex`), respects `requiresAttention` flag (e.g., highlight settings if Dexcom linking needed).
- Types: `ShellNavLink`.
- Props: `links: ShellNavLink[]`, `activePath: string`, `onNavigate: (link: ShellNavLink) => void`.

### UserMenuComponent

- Component description: Avatar button with dropdown for account routes, terms confirmation, and logout.
- Main elements: Material menu anchored to button; menu items for `Account`, `Data Sources`, `System Info`, `Logout`.
- Handled interactions: Click/keyboard to open menu; selecting item triggers route or logout; ensures closing on selection.
- Handled validation: `Logout` disabled when request in progress; menu items conditionally visible based on feature flags (e.g., `EnableDexcom`).
- Types: `ShellUserSummary` (for avatar + display), `UserMenuItem` (id, label, route?, action?).
- Props: `user: ShellUserSummary`, `items: UserMenuItem[]`, `onSelect: (item: UserMenuItem) => void`.

## 5. Types

- `interface ShellUserSummary { id: string; displayName: string; email: string; avatarUrl?: string; hasDexcomLinked: boolean; }`
- `interface ShellNavLink { id: 'dashboard' | 'settings'; label: string; path: string; icon: string; ariaId: string; requiresAttention?: boolean; badgeCount?: number; }`
- `interface ShellStateVM { user: ShellUserSummary; navLinks: ShellNavLink[]; activePath: string; acknowledgedLegal: boolean; }`
- `interface UserMenuItem { id: 'account' | 'data-sources' | 'system-info' | 'logout'; label: string; route?: string; action?: 'logout'; }`

ViewModel types live in `app/core/shell/models` (or similar) and mirror the frontend state contracts exposed by `ShellStateService`.

## 6. State Management

- `ShellStateService` (injectable singleton) aggregates router info and user profile from existing frontend stores. Utilizes Angular Signals (`signal`, `computed`, `effect`) for reactive updates without network calls.
- Router integration: effect watching `router.events` updates `activePath` signal to highlight nav.
- No custom hook necessary; Angular signals/services fill role. Provide context to components via `input()` bindings from `AppShellLayoutComponent`.

## 7. Integration Notes

- The AppShell consumes state from existing frontend facades (`AuthFacade`, `NavigationService`) supplied by the host application. No HTTP requests originate from this view, and all actions dispatch to in-memory stores or router navigation.

## 8. User Interactions

- Navigation tab click → updates router path, sets active tab, and, when triggered via keyboard, moves focus to the main content region for accessibility.
- User menu selection: `Account` navigates to `/settings`; `Data Sources` to `/settings/data-sources`; `System Info` to `/settings/system`; `Logout` dispatches through `AuthFacade` and on success redirects to `/login`.
## 9. Conditions and Validation

- Auth condition: Only render shell if `AuthGuard` confirms valid session; otherwise reroute.
- Legal acknowledgement: If `acknowledgedLegal` false, surface blocking flow until user confirms.
- Dexcom linking attention: When `hasDexcomLinked` false, set `requiresAttention` on Settings nav to encourage linking.

## 10. Error Handling

- Facade errors (auth): surface a global message categorized per PRD buckets (`Report bug`, `Check connection`, etc.) derived from the facade state; provide retry affordances where appropriate.
- Guard fallback: If route activation fails mid-navigation, show a blocking message and redirect to `/login`.

## 11. Implementation Steps

1. Define shell models (`ShellUserSummary`, `ShellNavLink`, etc.) under `app/core/shell/models` and align with the existing frontend state contracts.
2. Implement `ShellStateService` using signals and OnPush-friendly patterns; connect it to the host application's state stores (no HTTP wiring required).
3. Scaffold standalone components (`AppShellLayoutComponent`, `AppHeaderComponent`, `NavTabsComponent`, `UserMenuComponent`) with Angular 19 CLI and apply dark theme styles via Tailwind + Angular Material theming.
4. Configure routing: wrap protected routes with shell layout, apply `AuthGuard`, ensure `/settings` redirects to `/settings/data-sources`, add deep link for Dexcom callback.
5. Integrate services within `AppShellLayoutComponent`: subscribe to signals, connect to router events, pass view models to child components via `input()`.
6. Ensure accessibility: nav keyboard interactions, aria roles for status messaging, proper i18n annotations (`i18n="@@app.shell.*"`).
7. Write unit tests for services and components (header navigation), plus component harness tests for nav behaviours.
8. Update end-to-end test plan to cover login → AppShell display and navigation between Dashboard and Settings.

