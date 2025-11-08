# E2E Testing with Playwright

This directory contains End-to-End (E2E) tests for the Glyloop web application using Playwright.

## 📁 Directory Structure

```
e2e/
├── pages/                    # Page Object Models
│   ├── login.page.ts        # Login page interactions
│   └── dashboard.page.ts    # Dashboard page interactions
├── login-flow.spec.ts       # Login flow test suite
├── sample.spec.ts           # Sample test demonstrating best practices
├── TEST_IDS.md             # Reference for all data-testid attributes
└── README.md               # This file
```

## 🚀 Running Tests

### Run all tests
```bash
npm run test:e2e
```

### Run specific test file
```bash
npx playwright test e2e/login-flow.spec.ts
```

### Run tests in headed mode (see browser)
```bash
npx playwright test --headed
```

### Run tests in debug mode
```bash
npx playwright test --debug
```

### Run tests in UI mode
```bash
npx playwright test --ui
```

## 📝 Test Suites

### Login Flow Tests (`login-flow.spec.ts`)

Comprehensive test coverage for login functionality:

#### ✅ Login Page Display
- Verifies all page elements are visible
- Checks page title and headings
- Validates register link presence

#### ✅ Successful Login
- Tests login with valid credentials (`test@test.com` / `TestPassword123!`)
- Verifies redirect to dashboard
- Tests complete login and logout flow

#### ✅ Failed Login Attempts
- Invalid email address
- Incorrect password
- Empty password field
- Completely wrong credentials

#### ✅ Email Field Validation
- Empty email field
- Invalid email formats (various patterns)
- Valid email acceptance
- Required field validation

#### ✅ Password Field Validation
- Empty password field
- Required field validation
- Non-empty password acceptance

#### ✅ Password Visibility Toggle
- Toggle password visibility on/off
- Verify password type changes (password ↔ text)
- Button presence and functionality

#### ✅ Form Validation and Feedback
- Disabled state for invalid forms
- Enabled state for valid forms
- Loading state during submission

#### ✅ Accessibility
- Proper labels for form fields
- Button labels
- Keyboard navigation support

#### ✅ Responsive Design
- Mobile viewport (iPhone SE - 375x667)
- Tablet viewport (iPad - 768x1024)
- Desktop viewport (1920x1080)

## 🎯 Page Object Model

The tests use the Page Object Model (POM) pattern for maintainability:

### LoginPage (`pages/login.page.ts`)
Encapsulates all interactions with the login page:
- Navigation
- Form filling
- Validation checking
- Error message retrieval

**Example usage:**
```typescript
const loginPage = new LoginPage(page);
await loginPage.navigateToLogin();
await loginPage.login('test@test.com', 'TestPassword123!');
```

### DashboardPage (`pages/dashboard.page.ts`)
Encapsulates all interactions with the dashboard:
- Navigation
- Chart interactions
- Event management
- User menu and logout

**Example usage:**
```typescript
const dashboardPage = new DashboardPage(page);
await dashboardPage.navigateToDashboard();
await dashboardPage.clickAddEvent();
await dashboardPage.logout();
```

## 🏷️ Test IDs

All interactive elements have `data-testid` attributes for reliable test selectors. See [TEST_IDS.md](./TEST_IDS.md) for a complete reference.

### Example Test IDs:
- `login-email-input` - Email input field
- `login-password-input` - Password input field
- `login-submit-button` - Login button
- `dashboard-title` - Dashboard page title
- `add-event-button` - Add event button

## 🧪 Test Credentials

### Valid Account
- **Email:** `test@test.com`
- **Password:** `TestPassword123!`

### Test Scenarios
The test suite covers:
- ✅ Valid login
- ❌ Invalid email
- ❌ Wrong password
- ❌ Empty fields
- ❌ Invalid email formats
- 🔄 Password visibility toggle
- 📱 Responsive design
- ♿ Accessibility

## 📋 Best Practices

This test suite follows Playwright best practices:

1. **Page Object Model** - Encapsulates page interactions
2. **Explicit Waits** - Uses `waitFor` and `waitForURL` for reliable tests
3. **Data Test IDs** - Uses `data-testid` for stable selectors
4. **Descriptive Names** - Clear test descriptions
5. **Arrange-Act-Assert** - Clear test structure
6. **Independent Tests** - Each test is self-contained
7. **Browser Context Isolation** - Tests don't interfere with each other

## 🔧 Configuration

Playwright configuration is in `playwright.config.ts` at the project root.

### Key Settings:
- **Browser:** Chromium only (as per guidelines)
- **Base URL:** Configured for local development
- **Timeout:** 30 seconds default
- **Retries:** 2 on CI, 0 locally
- **Parallel:** True for faster execution

## 📊 Reporting

After running tests, view the HTML report:
```bash
npx playwright show-report
```

## 🐛 Debugging Tests

### Step-through debugging:
```bash
npx playwright test --debug
```

### Generate trace:
```bash
npx playwright test --trace on
```

### View trace:
```bash
npx playwright show-trace trace.zip
```

## 📚 Additional Resources

- [Playwright Documentation](https://playwright.dev)
- [Best Practices Guide](../docs/playwright-e2e-testing.mdc)
- [Test IDs Reference](./TEST_IDS.md)

## ✨ Adding New Tests

When adding new test cases:

1. **Add data-testid attributes** to components
2. **Update page objects** in `/e2e/pages/`
3. **Create or update spec files** in `/e2e/`
4. **Document test IDs** in `TEST_IDS.md`
5. **Follow naming conventions** for consistency

### Example:
```typescript
test('should do something', async ({ page }) => {
  // Arrange - Set up test data and state
  const loginPage = new LoginPage(page);
  await loginPage.navigateToLogin();
  
  // Act - Perform the action
  await loginPage.fillEmail('test@test.com');
  
  // Assert - Verify the outcome
  await expect(loginPage.emailInput).toHaveValue('test@test.com');
});
```

## 🎉 Happy Testing!

For questions or issues, please refer to the main project documentation or contact the development team.
