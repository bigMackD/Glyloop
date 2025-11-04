# Frontend Testing Guide

Complete guide for testing the Glyloop Angular application using Jest and Playwright.

## Table of Contents

- [Unit Testing with Jest](#unit-testing-with-jest)
- [E2E Testing with Playwright](#e2e-testing-with-playwright)
- [Installation](#installation)
- [Running Tests](#running-tests)
- [Best Practices](#best-practices)

## Unit Testing with Jest

### Overview

We use Jest for unit testing Angular components, services, and utilities.

**Key Features:**
- Fast test execution
- TypeScript support
- Rich assertion library
- Snapshot testing
- Code coverage reporting

### Test Structure

```typescript
describe('ComponentName', () => {
  beforeEach(() => {
    // Setup
  });

  afterEach(() => {
    // Cleanup
  });

  it('should do something', () => {
    // Arrange
    // Act
    // Assert
  });
});
```

### Testing Components

```typescript
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MyComponent } from './my.component';

describe('MyComponent', () => {
  let component: MyComponent;
  let fixture: ComponentFixture<MyComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MyComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(MyComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
```

### Testing Services

```typescript
import { TestBed } from '@angular/core/testing';
import { MyService } from './my.service';

describe('MyService', () => {
  let service: MyService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(MyService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
```

### Mocking Dependencies

```typescript
// Mock service
const mockService = {
  getData: jest.fn().mockReturnValue(of({ data: 'test' }))
};

// Use in tests
TestBed.configureTestingModule({
  providers: [
    { provide: MyService, useValue: mockService }
  ]
});
```

### Testing Async Code

```typescript
// Promises
it('should handle promises', async () => {
  const result = await service.getData();
  expect(result).toBeDefined();
});

// Observables
it('should handle observables', (done) => {
  service.getData().subscribe(result => {
    expect(result).toBeDefined();
    done();
  });
});
```

### Snapshot Testing

```typescript
it('should match snapshot', () => {
  const component = new MyComponent();
  expect(component).toMatchSnapshot();
});
```

## E2E Testing with Playwright

### Overview

Playwright tests verify complete user flows and application behavior.

**Key Features:**
- Browser automation (Chromium)
- Page Object Model
- Network interception
- Visual regression testing
- Parallel execution

### Test Structure

```typescript
import { test, expect } from '@playwright/test';

test.describe('Feature', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
  });

  test('should work', async ({ page }) => {
    // Test code
  });
});
```

See [e2e/README.md](./e2e/README.md) for detailed E2E testing guide.

## Installation

### Install Dependencies

```bash
cd Glyloop.Client/glyloop-web
npm install
```

This installs:
- Jest and jest-preset-angular
- @testing-library/angular
- @playwright/test
- All type definitions

### Install Playwright Browsers

```bash
npx playwright install chromium
```

## Running Tests

### Jest Unit Tests

```bash
# Run all tests
npm test

# Watch mode
npm run test:watch

# With coverage
npm run test:coverage

# Clear cache
npm run test:clear-cache
```

### Playwright E2E Tests

```bash
# Run all E2E tests
npm run e2e

# UI mode (interactive)
npm run e2e:ui

# Headed mode (see browser)
npm run e2e:headed

# Debug mode
npm run e2e:debug
```

## Configuration

### Jest Configuration

**jest.config.js:**
```javascript
module.exports = {
  preset: 'jest-preset-angular',
  setupFilesAfterEnv: ['<rootDir>/setup-jest.ts'],
  testEnvironment: 'jsdom',
  collectCoverage: true,
  coverageDirectory: 'coverage',
  coverageThreshold: {
    global: {
      branches: 70,
      functions: 70,
      lines: 70,
      statements: 70,
    },
  },
};
```

### Playwright Configuration

**playwright.config.ts:**
```typescript
export default defineConfig({
  testDir: './e2e',
  use: {
    baseURL: 'http://localhost:4200',
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
});
```

## Best Practices

### General

1. ✅ Write tests before or alongside code (TDD)
2. ✅ Keep tests simple and focused
3. ✅ Use descriptive test names
4. ✅ Test behavior, not implementation
5. ✅ Maintain test independence
6. ✅ Use AAA pattern (Arrange-Act-Assert)

### Jest-Specific

1. ✅ Use `describe` blocks to organize related tests
2. ✅ Leverage mock functions for isolating units
3. ✅ Use `beforeEach` and `afterEach` for setup/teardown
4. ✅ Implement code coverage with meaningful targets
5. ✅ Use snapshot testing sparingly (only for stable UI)
6. ✅ Mock external dependencies
7. ✅ Use `mockResolvedValue` and `mockRejectedValue` for async

### Playwright-Specific

1. ✅ Use Page Object Model for maintainability
2. ✅ Use resilient locators (data-testid, role-based)
3. ✅ Leverage browser contexts for isolation
4. ✅ Wait for network idle before assertions
5. ✅ Use specific expect matchers
6. ✅ Implement visual regression sparingly
7. ✅ Test API endpoints directly when appropriate
8. ✅ Use parallel execution for faster runs

## Coverage Targets

- **Components**: ≥70%
- **Services**: ≥70%
- **Utilities**: ≥80%
- **Overall**: ≥70%

## Debugging

### Jest

```bash
# Run specific test file
npm test -- sample-component.spec.ts

# Run tests matching pattern
npm test -- --testNamePattern="should increment"

# Debug in VS Code
# Set breakpoint and run "Debug Jest Tests"
```

### Playwright

```bash
# Debug mode
npm run e2e:debug

# Show test report
npx playwright show-report

# Show trace
npx playwright show-trace test-results/trace.zip
```

## CI/CD Integration

Tests run automatically in GitHub Actions:

```yaml
- name: Install dependencies
  run: npm ci

- name: Run Jest tests
  run: npm run test:coverage

- name: Install Playwright
  run: npx playwright install --with-deps chromium

- name: Run E2E tests
  run: npm run e2e
```

## Sample Test Files

Reference these files for comprehensive examples:

**Jest:**
- `src/app/sample-component.spec.ts` - Component testing patterns
- `src/app/sample-service.spec.ts` - Service testing patterns

**Playwright:**
- `e2e/sample.spec.ts` - E2E testing patterns
- `e2e/pages/login.page.ts` - Page Object Model example
- `e2e/pages/dashboard.page.ts` - Complex UI interactions

## Resources

- [Jest Documentation](https://jestjs.io/)
- [Angular Testing Guide](https://angular.dev/guide/testing)
- [Testing Library](https://testing-library.com/docs/angular-testing-library/intro/)
- [Playwright Documentation](https://playwright.dev/)
- [E2E Testing Guide](./e2e/README.md)

## Troubleshooting

### Jest Issues

**Problem**: Tests fail with module resolution errors
```bash
# Solution: Clear Jest cache
npm run test:clear-cache
```

**Problem**: Coverage not generated
```bash
# Solution: Run with coverage flag
npm run test:coverage
```

### Playwright Issues

**Problem**: Browser not installed
```bash
# Solution: Install browsers
npx playwright install chromium
```

**Problem**: Tests timeout
```bash
# Solution: Increase timeout in playwright.config.ts
export default defineConfig({
  timeout: 60000, // 60 seconds
});
```

**Problem**: Port 4200 already in use
```bash
# Solution: Kill the process or change port
# Windows: netstat -ano | findstr :4200
# Mac/Linux: lsof -ti:4200 | xargs kill
```

## Next Steps

1. Review sample test files
2. Write tests for existing components
3. Set up CI/CD integration
4. Configure code coverage reporting
5. Implement visual regression tests
6. Add accessibility testing

For questions or issues, consult the team or file an issue in the repository.

