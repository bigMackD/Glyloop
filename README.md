# Glyloop

[![Status](https://img.shields.io/badge/status-WIP-orange.svg)](./)
[![Angular](https://img.shields.io/badge/Angular-20.3.x-dd0031.svg)](./Glyloop.Client/glyloop-web/package.json)
[![.NET](https://img.shields.io/badge/.NET-8.0-512bd4.svg)](./Glyloop.API/Glyloop.API.sln)
[![TypeScript](https://img.shields.io/badge/TypeScript-5.9.x-3178c6.svg)](./Glyloop.Client/glyloop-web/package.json)
[![License](https://img.shields.io/badge/license-TBD-lightgrey.svg)](./)

### Table of Contents
- [Project name](#glyloop)
- [Project description](#project-description)
- [Tech stack](#tech-stack)
- [Getting started locally](#getting-started-locally)
- [Available scripts](#available-scripts)
- [Project scope](#project-scope)
- [Project status](#project-status)
- [License](#license)

## Project description
Glyloop is a desktop-first web application for adults living with Type 1 Diabetes who use Dexcom CGM. The MVP centers on a tight daily loop: view current CGM glucose, log events (Food, Insulin, Exercise, Notes), and review their impact on an interactive time-series chart. The MVP uses Dexcom Sandbox as the only external data source, with 5-minute polling and a recent-window bootstrap. Events are immutable after creation. Security uses ASP.NET Core Identity with JWT for the SPA, secure httpOnly cookies, SameSite=Lax, and CSRF protection for state-changing calls.

Key chart behaviors include fixed time ranges (1h/3h/5h/8h/12h/24h), crosshair interactions, gaps rendered as breaks, a dynamic y-clamp [50, 350] mg/dL, and overlays for event types. Time in Range (TIR) is computed for the active window with one editable range (default 70–180 mg/dL). A +2h outcome rule for Food displays a CGM value at event_time + 120 minutes if a sample exists within ±5 minutes; otherwise N/A with an explanatory tooltip.

For full context and user stories, see the PRD at `.ai/prd.md`.

## Tech stack
- Frontend
  - Angular 20.3.x, Angular Material/CDK
  - TypeScript ~5.9.x
  - Tailwind CSS 3.4.x
- Backend
  - .NET 8 (ASP.NET Core 8)
  - Entity Framework Core
  - CQRS and Domain-Driven Design
  - JWT auth with secure cookie sessions and CSRF for state changes
- Data and Integrations
  - Dexcom Sandbox API (OAuth, 5-minute polling)
  - PostgreSQL (primary database)
  - Future: Nightscout (post-MVP)
- DevOps and CI/CD
  - Docker (multi-stage builds) and docker-compose (planned)
  - GitHub Actions (planned for build/test/deploy)
- References
  - Tech stack notes: `.ai/tech-stack.md`

## Getting started locally

### Prerequisites
- Node.js 20+ and npm
- .NET 8 SDK
- PostgreSQL (if running the API with persistence)
- Windows, macOS, or Linux

Project layout:
- Backend API: `Glyloop.API/`
- Frontend web app: `Glyloop.Client/glyloop-web/`

### Frontend (Angular)
```bash
cd Glyloop.Client/glyloop-web
npm install
npm start
# App runs at http://localhost:4200
```

### Backend (ASP.NET Core 8)
```bash
cd Glyloop.API/Glyloop.API
dotnet restore
dotnet run
# API listens on the port defined in launchSettings/appsettings (commonly http://localhost:5000 or similar)
```

Configure API settings as needed:
- Database connection string (PostgreSQL)
- Dexcom Sandbox OAuth credentials
- Security settings (JWT, cookie, CSRF)
These typically live in `appsettings.Development.json`, `appsettings.json`, or environment variables.

### Docker (planned per MVP)
The PRD specifies multi-stage Dockerfiles for the API and Web, with docker-compose orchestration and health checks. Docker assets will be added as the MVP matures.

## Available scripts

In `Glyloop.Client/glyloop-web/package.json`:
- `ng`: Run Angular CLI.
- `start`: Start the dev server (`ng serve`).
- `build`: Production build (`ng build`).
- `watch`: Development watch build.
- `test`: Run unit tests (Karma/Jasmine).
- `lint`: Lint the project (ESLint).
- `lint:fix`: Lint and auto-fix.

## Project scope

In scope for MVP:
- Dexcom Sandbox linking (OAuth), 5-minute polling, resilient sync with backoff and token refresh.
- Event logging for Food, Insulin (fast/long), Exercise, Notes with validation and overlays.
- +2h outcome for Food with strict ±5-minute tolerance.
- TIR calculation for the active chart window with one editable range (default 70–180 mg/dL).
- History view with date range and type/tag filters.
- Dockerized deployment with compose (planned).
- Security: Identity + JWT, password policy (min 12 chars), tokens encrypted at rest.

Out of scope for MVP:
- Demo mode and related UI.
- Dedicated sync status chip and detailed status page.
- Initial 7-day backfill and user-triggered older loads.
- Duplicate-prevention UX and server idempotency exposure.
- Event editing, deletion, backdating.
- System Info flags page.
- Full observability/alerting dashboards.
- Units conversion UI and persisted preferred chart ranges.
- Nightscout integration and data exports (post-MVP).
- Advanced accessibility beyond adequate contrast.
- Mobile apps and offline/PWA.
- Full support desk workflow.

See full details and user stories in `.ai/prd.md`.

## Project status
- Status: Work in progress (MVP).
- Frontend scaffolded with Angular 20.3.x, ESLint, Tailwind.
- Backend solution initialized for .NET 8.
- CI (GitHub Actions) and Dockerfiles are planned per PRD.
- Success metrics target reliable Dexcom linking, chart freshness, validated event logging, and +2h outcomes; operational cadence at 5 minutes with healthy restarts.

## License
No license has been specified yet. Until a LICENSE file is added, all rights are reserved by the project owner.