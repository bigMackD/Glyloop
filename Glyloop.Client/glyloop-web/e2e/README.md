# E2E Testing Guide with Playwright

End-to-end tests for Glyloop web application using Playwright.

## Overview

This directory contains E2E tests that verify complete user flows and application functionality from a user's perspective.

## Structure

```
e2e/
├── pages/              # Page Object Models
│   ├── base.page.ts   # Base page with common methods
│   ├── login.page.ts  # Login page interactions
│   └── dashboard.page.ts
├── sample.spec.ts      # Sample test suite
└── README.md
```

## Page Object Model (POM)

We use the Page Object Model pattern to:
- Centralize UI element selectors
- Reduce code duplication
- Make tests more maintainable
- Abstract implementation details

### Example Page Object

```typescript
import { Page, Locator } from '@playwright/test';
import { BasePage } from './base.page';

export class LoginPage extends BasePage {
  readonly emailInput: Locator;
  readonly passwordInput: Locator;
  readonly loginButton: Locator;

  constructor(page: Page) {
    super(page);
    this.emailInput = page.locator('input[type="email"]');
    this.passwordInput = page.locator('input[type="password"]');
    this.loginButton = page.locator('button[type="submit"]');
  }

  async login(email: string, password: string): Promise<void> {
    await this.fillInput(this.emailInput, email);
    await this.fillInput(this.passwordInput, password);
    await this.clickElement(this.loginButton);
  }
}
```

## Writing Tests

### Basic Test Structure

```typescript
import { test, expect } from '@playwright/test';
import { LoginPage } from './pages/login.page';

test.describe('Authentication', () => {
  let loginPage: LoginPage;

  test.beforeEach(async ({ page }) => {
    loginPage = new LoginPage(page);
    await loginPage.navigateToLogin();
  });

  test('should login successfully', async ({ page }) => {
    // Arrange
    const email = 'test@example.com';
    const password = 'Password123!';

    // Act
    await loginPage.login(email, password);

    // Assert
    await expect(page).toHaveURL(/dashboard/);
  });
});
```

## Best Practices

### 1. Use Resilient Locators

**Good:**
```typescript
// Data test IDs (most resilient)
page.locator('[data-testid="submit-button"]')

// Role-based
page.getByRole('button', { name: 'Submit' })

// Placeholder
page.getByPlaceholder('Enter email')

// Text content
page.getByText('Welcome back')
```

**Avoid:**
```typescript
// CSS classes (fragile)
page.locator('.btn-primary')

// XPath (hard to maintain)
page.locator('//div[@class="container"]//button')
```

### 2. Use Browser Contexts for Isolation

```typescript
test('isolated test', async ({ browser }) => {
  const context = await browser.newContext();
  const page = await context.newPage();
  
  // Test code here
  
  await context.close();
});
```

### 3. Wait for Network Idle

```typescript
await page.goto('/dashboard');
await page.waitForLoadState('networkidle');
```

### 4. Use Specific Assertions

```typescript
// Good - specific assertions
await expect(loginPage.emailInput).toBeVisible();
await expect(page).toHaveURL(/dashboard/);
await expect(element).toHaveText('Expected text');

// Avoid - generic assertions
expect(await element.isVisible()).toBe(true);
```

### 5. Handle Async Operations

```typescript
// Wait for element
await page.waitForSelector('[data-testid="chart"]');

// Wait for URL change
await page.waitForURL('**/dashboard');

// Wait for API response
await page.waitForResponse(response => 
  response.url().includes('/api/events') && response.status() === 200
);
```

## Running Tests

### All Tests
```bash
npm run e2e
```

### UI Mode (Interactive)
```bash
npm run e2e:ui
```

### Headed Mode (See Browser)
```bash
npm run e2e:headed
```

### Debug Mode
```bash
npm run e2e:debug
```

### Specific Test File
```bash
npx playwright test sample.spec.ts
```

### Specific Test
```bash
npx playwright test -g "should login successfully"
```

## Debugging

### 1. Playwright Inspector
```bash
npm run e2e:debug
```

### 2. Screenshots on Failure
Automatically captured in `test-results/`

### 3. Video Recording
Captured on failure, stored in `test-results/`

### 4. Trace Viewer
```bash
npx playwright show-trace test-results/trace.zip
```

### 5. Codegen Tool
```bash
npx playwright codegen http://localhost:4200
```

## Visual Testing

### Screenshots
```typescript
await expect(page).toHaveScreenshot('dashboard.png');
```

### Full Page Screenshots
```typescript
await expect(page).toHaveScreenshot('full-page.png', {
  fullPage: true,
  animations: 'disabled',
});
```

## API Testing

Test backend APIs directly:

```typescript
test('should validate API', async ({ request }) => {
  const response = await request.get('/api/events');
  
  expect(response.ok()).toBeTruthy();
  expect(response.status()).toBe(200);
  
  const data = await response.json();
  expect(data).toHaveProperty('events');
});
```

## Parallel Execution

Tests run in parallel by default. Control parallelism:

### In Config
```typescript
// playwright.config.ts
export default defineConfig({
  workers: 4, // Number of parallel workers
});
```

### Per Suite
```typescript
test.describe.parallel('Parallel tests', () => {
  test('test 1', async ({ page }) => { /* ... */ });
  test('test 2', async ({ page }) => { /* ... */ });
});
```

## CI/CD Integration

Tests run in GitHub Actions:
```yaml
- name: Install Playwright
  run: npx playwright install --with-deps chromium

- name: Run E2E tests
  run: npm run e2e
```

## Tips

1. **Keep tests independent** - Each test should work in isolation
2. **Use data-testid attributes** - Add them to critical elements
3. **Avoid hard-coded waits** - Use Playwright's auto-waiting
4. **Clean up after tests** - Use afterEach hooks
5. **Test user flows, not implementation** - Focus on what users do
6. **Use meaningful test names** - Describe the expected behavior
7. **Group related tests** - Use describe blocks

## Common Patterns

### Login Before Each Test
```typescript
test.beforeEach(async ({ page }) => {
  await page.goto('/login');
  await page.fill('[type="email"]', 'user@example.com');
  await page.fill('[type="password"]', 'password');
  await page.click('button[type="submit"]');
  await page.waitForURL('**/dashboard');
});
```

### Test with Different Viewports
```typescript
test('mobile view', async ({ page }) => {
  await page.setViewportSize({ width: 375, height: 667 });
  // Test mobile-specific behavior
});
```

### Test with Authentication
```typescript
test.use({ storageState: 'auth.json' });

test('authenticated test', async ({ page }) => {
  // Already logged in
  await page.goto('/dashboard');
});
```

## Resources

- [Playwright Documentation](https://playwright.dev)
- [Best Practices](https://playwright.dev/docs/best-practices)
- [API Reference](https://playwright.dev/docs/api/class-playwright)
- [Sample Tests](./sample.spec.ts)

