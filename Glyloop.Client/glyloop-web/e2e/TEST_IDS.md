# Playwright Test IDs Reference

This document provides a comprehensive reference of all `data-testid` attributes used in the Glyloop application for Playwright E2E testing.

## Login Page

### Page Elements
- `login-page` - Main login page container
- `login-logo` - Glyloop logo image
- `login-title` - Page title heading
- `login-subtitle` - Page subtitle text

### Login Form
- `login-form` - Login form container
- `login-email-input` - Email input field
- `login-email-error` - Email validation error message
- `login-password-input` - Password input field
- `login-password-error` - Password validation error message
- `login-password-visibility-toggle` - Button to toggle password visibility
- `login-submit-button` - Login submit button
- `login-submit-loading` - Loading spinner during submission
- `login-error-message` - Server error message banner

### Footer Links
- `register-link` - Link to registration page
- `registration-success-message` - Success message after registration

## Dashboard Page

### Main Elements
- `dashboard-page` - Main dashboard container
- `dashboard-title` - Dashboard page title
- `add-event-button` - Button to open add event modal
- `glucose-chart` - Glucose chart container
- `events-list` - Events history list container

### User Menu
- `user-menu-button` - User menu trigger button
- `user-menu-account` - Account menu item
- `user-menu-data-sources` - Data sources menu item
- `user-menu-system-info` - System info menu item
- `user-menu-logout` - Logout menu item

## Add Event Modal

### Modal Container
- `add-event-dialog` - Add event modal overlay
- `add-event-title` - Modal title
- `add-event-close-button` - Close button (X)
- `add-event-cancel-button` - Cancel button
- `add-event-submit-button` - Submit button
- `add-event-error-message` - Error message banner

### Event Tabs
- `event-tabs` - Tab group container
- `food-tab` - Food event tab
- `insulin-tab` - Insulin event tab
- `exercise-tab` - Exercise event tab
- `note-tab` - Note event tab

### Food Form
- `food-form` - Food event form container
- `food-carbs-input` - Carbohydrates input field (grams)

### Insulin Form
- `insulin-form` - Insulin event form container
- `insulin-type-select` - Insulin type dropdown
- `insulin-units-input` - Insulin units input field

### Exercise Form
- `exercise-form` - Exercise event form container

### Note Form
- `note-form` - Note event form container

## Usage in Tests

### Example: Login Flow
```typescript
import { test, expect } from '@playwright/test';
import { LoginPage } from './pages/login.page';

test('should login successfully', async ({ page }) => {
  const loginPage = new LoginPage(page);
  
  await loginPage.navigateToLogin();
  await loginPage.emailInput.fill('test@test.com');
  await loginPage.passwordInput.fill('TestPassword123!');
  await loginPage.loginButton.click();
  
  await page.waitForURL('**/dashboard');
  await expect(page).toHaveURL(/dashboard/);
});
```

### Example: Dashboard Interaction
```typescript
import { test, expect } from '@playwright/test';
import { DashboardPage } from './pages/dashboard.page';

test('should open add event dialog', async ({ page }) => {
  const dashboardPage = new DashboardPage(page);
  
  await dashboardPage.navigateToDashboard();
  await dashboardPage.clickAddEvent();
  
  await expect(dashboardPage.addEventDialog).toBeVisible();
});
```

## Best Practices

1. **Consistent Naming**: Use kebab-case for all test IDs
2. **Descriptive Names**: Test IDs should clearly indicate the element's purpose
3. **Scope Prefixes**: Use prefixes like `login-`, `dashboard-`, `add-event-` to indicate context
4. **Avoid Dynamic IDs**: Test IDs should be static and not change based on state
5. **Error Messages**: Include test IDs on all error messages for validation testing
6. **Forms**: Include test IDs on form containers, inputs, and submit buttons
7. **Modals/Dialogs**: Include test IDs on overlay, content, and action buttons

## Maintenance

When adding new components:
1. Add `data-testid` attributes to all interactive elements
2. Update this document with new test IDs
3. Update page object models in `/e2e/pages/`
4. Add test cases in relevant spec files

## Test Credentials

For E2E testing, use the following test accounts:

### Valid Credentials
- Email: `test@test.com`
- Password: `TestPassword123!`

### Invalid Credentials (for negative testing)
- Various invalid emails: `invalid@example.com`, `notanemail`, etc.
- Wrong passwords: Any password other than the valid one

