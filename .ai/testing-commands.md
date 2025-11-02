# Testing Commands Quick Reference

Quick reference for all testing commands in Glyloop project.

## Backend Testing (NUnit)

### Run Tests
```bash
# All tests
dotnet test

# Specific project
dotnet test Tests/Glyloop.Domain.Tests
dotnet test Tests/Glyloop.Application.Tests
dotnet test Tests/Glyloop.Infrastructure.Tests
dotnet test Tests/Glyloop.API.Tests

# By category
dotnet test --filter Category=Unit
dotnet test --filter Category=Integration

# Specific test
dotnet test --filter "FullyQualifiedName~SampleDomainTests"
```

### Code Coverage
```bash
# Generate coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=lcov

# With specific output
dotnet test /p:CollectCoverage=true /p:CoverletOutput=./coverage/ /p:CoverletOutputFormat=lcov
```

### Build & Restore
```bash
# Restore packages
dotnet restore

# Build
dotnet build

# Clean
dotnet clean
```

## Frontend Testing (Jest)

### Run Unit Tests
```bash
# All tests
npm test

# Watch mode (auto-rerun on changes)
npm run test:watch

# With coverage
npm run test:coverage

# Clear cache
npm run test:clear-cache

# Specific file
npm test -- sample-component.spec.ts

# Matching pattern
npm test -- --testNamePattern="should increment"
```

### Debug Tests
```bash
# Debug in Node
node --inspect-brk node_modules/.bin/jest --runInBand

# VS Code: Set breakpoint and run "Debug Jest Tests"
```

## Frontend E2E Testing (Playwright)

### Run Tests
```bash
# All E2E tests
npm run e2e

# UI mode (interactive)
npm run e2e:ui

# Headed mode (see browser)
npm run e2e:headed

# Debug mode
npm run e2e:debug

# Specific file
npx playwright test sample.spec.ts

# Specific test
npx playwright test -g "should login successfully"
```

### Playwright Tools
```bash
# Install browsers
npx playwright install chromium
npx playwright install --with-deps chromium  # CI

# Codegen (record tests)
npx playwright codegen http://localhost:4200

# Show report
npx playwright show-report

# Show trace
npx playwright show-trace test-results/trace.zip
```

## Development Server

### Frontend
```bash
# Start dev server
npm start

# Custom port
ng serve --port 4300
```

### Backend
```bash
# Run API
cd Glyloop.API/Glyloop.API
dotnet run

# Watch mode
dotnet watch run
```

## Combined Workflows

### Full Test Suite
```bash
# Backend
cd Glyloop.API
dotnet test /p:CollectCoverage=true

# Frontend
cd ../Glyloop.Client/glyloop-web
npm run test:coverage
npm run e2e
```

### Pre-Commit
```bash
# Backend
dotnet build
dotnet test --filter Category=Unit

# Frontend
npm run lint:fix
npm test
```

### CI Pipeline
```bash
# Backend
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release --no-build

# Frontend
npm ci
npm run test:coverage
npx playwright install --with-deps chromium
npm run e2e
```

## Code Quality

### Linting
```bash
# Frontend
npm run lint
npm run lint:fix

# Backend (implicit with build)
dotnet build
```

### Format
```bash
# Frontend (Prettier)
npx prettier --write "src/**/*.{ts,html,scss}"

# Backend (dotnet format - if configured)
dotnet format
```

## Docker (for TestContainers)

### Check Docker
```bash
# Verify Docker is running
docker ps

# Start Docker Desktop (manual)
```

### PostgreSQL Container
```bash
# Pull image (optional, TestContainers does this)
docker pull postgres:16

# List containers
docker ps -a
```

## IDE Shortcuts

### Visual Studio / Rider
- `Ctrl+R, A` - Run all tests
- `Ctrl+R, T` - Run tests in current context
- `Ctrl+R, D` - Debug tests in current context
- `Ctrl+R, L` - Repeat last test run

### VS Code
- Install extensions: C# Dev Kit, Jest Runner, Playwright Test
- Use Test Explorer in sidebar
- Click play button next to tests

## Coverage Thresholds

| Layer | Target |
|-------|--------|
| Backend Domain | ≥80% |
| Backend Application | ≥80% |
| Backend Infrastructure | ≥60% |
| Backend API | ≥70% |
| Frontend Components | ≥70% |
| Frontend Services | ≥70% |

## Test File Patterns

### Backend
- `*Tests.cs` - Test classes
- `Method_ShouldDoX_WhenY` - Test method naming

### Frontend
- `*.spec.ts` - Unit test files
- `*.e2e.ts` or `e2e/*.spec.ts` - E2E test files
- `*.page.ts` - Page Object Models

## Quick Setup

### First Time Setup
```bash
# Backend
cd Glyloop.API
dotnet restore
dotnet build
dotnet test

# Frontend
cd ../Glyloop.Client/glyloop-web
npm install
npx playwright install chromium
npm test
npm run e2e
```

### Daily Development
```bash
# Terminal 1: Frontend dev server
cd Glyloop.Client/glyloop-web
npm start

# Terminal 2: Backend API
cd Glyloop.API/Glyloop.API
dotnet watch run

# Terminal 3: Tests in watch mode
npm run test:watch
```

## Common Issues & Solutions

### Backend
```bash
# Issue: "Cannot find NUnit"
dotnet restore
dotnet build

# Issue: "TestContainers can't start"
# Ensure Docker Desktop is running
docker ps
```

### Frontend
```bash
# Issue: "Module not found"
npm install

# Issue: "Playwright browser not found"
npx playwright install chromium

# Issue: "Port 4200 in use"
# Windows: netstat -ano | findstr :4200
# Mac/Linux: lsof -ti:4200 | xargs kill
```

## Documentation Links

- [Backend Testing Guide](../Glyloop.API/Tests/README.md)
- [Frontend Testing Guide](../Glyloop.Client/glyloop-web/TESTING.md)
- [E2E Testing Guide](../Glyloop.Client/glyloop-web/e2e/README.md)
- [Full Setup Guide](../TESTING_SETUP.md)

---

**Keep this file handy for quick reference during development!**

