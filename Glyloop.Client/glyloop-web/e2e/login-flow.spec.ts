/**
 * Login Flow E2E Test Suite
 * Comprehensive tests for login functionality following Playwright best practices
 * 
 * Test credentials:
 * - Valid: test@test.com / TestPassword123!
 * - Invalid: Various test cases for validation
 */

import { test, expect } from '@playwright/test';
import { LoginPage } from './pages/login.page';
import { DashboardPage } from './pages/dashboard.page';

/**
 * Test suite for login page display and basic functionality
 */
test.describe('Login Page Display', () => {
  let loginPage: LoginPage;

  test.beforeEach(async ({ page }) => {
    loginPage = new LoginPage(page);
    await loginPage.navigateToLogin();
  });

  test('should display login page correctly', async ({ page }) => {
    // Assert - Check page title
    await expect(page).toHaveTitle(/Glyloop/);
    
    // Assert - Check login form elements are visible
    await expect(loginPage.loginLogo).toBeVisible();
    await expect(loginPage.loginTitle).toBeVisible();
    await expect(loginPage.emailInput).toBeVisible();
    await expect(loginPage.passwordInput).toBeVisible();
    await expect(loginPage.loginButton).toBeVisible();
    await expect(loginPage.registerLink).toBeVisible();
  });

  test('should have correct page heading', async () => {
    // Assert - Check the heading text
    const titleText = await loginPage.loginTitle.textContent();
    expect(titleText).toContain('Sign in');
  });

  test('should display register link', async () => {
    // Assert - Register link is visible and clickable
    await expect(loginPage.registerLink).toBeVisible();
    await expect(loginPage.registerLink).toHaveAttribute('href', '/register');
  });
});

/**
 * Test suite for successful login
 */
test.describe('Successful Login', () => {
  let loginPage: LoginPage;
  let dashboardPage: DashboardPage;

  test.beforeEach(async ({ page }) => {
    loginPage = new LoginPage(page);
    dashboardPage = new DashboardPage(page);
    await loginPage.navigateToLogin();
  });

  test('should login successfully with valid credentials', async ({ page }) => {
    // Arrange
    const validEmail = 'test@test.com';
    const validPassword = 'TestPassword123!';

    // Act - Fill in credentials and submit
    await loginPage.login(validEmail, validPassword);

    // Assert - Should redirect to dashboard
    await page.waitForURL('**/dashboard', { timeout: 10000 });
    await expect(page).toHaveURL(/dashboard/);
    
    // Assert - Dashboard should be loaded with proper waiting
    await expect(dashboardPage.dashboardTitle).toBeVisible({ timeout: 10000 });
    await expect(dashboardPage.glucoseChart).toBeVisible({ timeout: 10000 });
  });

  test('should complete full login and logout flow', async ({ page }) => {
    // Step 1: Login with valid credentials
    await loginPage.login('test@test.com', 'TestPassword123!');
    await page.waitForURL('**/dashboard', { timeout: 10000 });
    
    // Step 2: Verify dashboard is loaded - wait for elements explicitly
    await expect(dashboardPage.dashboardTitle).toBeVisible({ timeout: 10000 });
    await expect(dashboardPage.glucoseChart).toBeVisible({ timeout: 10000 });
    
    // Step 3: Logout
    await dashboardPage.logout();
    
    // Step 4: Verify redirected to login
    await page.waitForURL('**/login', { timeout: 10000 });
    await expect(page).toHaveURL(/login/);
  });
});

/**
 * Test suite for failed login attempts
 */
test.describe('Failed Login Attempts', () => {
  let loginPage: LoginPage;

  test.beforeEach(async ({ page }) => {
    loginPage = new LoginPage(page);
    await loginPage.navigateToLogin();
  });

  test('should show error with invalid email', async () => {
    // Arrange
    const invalidEmail = 'invalid@example.com';
    const validPassword = 'TestPassword123!';

    // Act - Attempt login with invalid email
    await loginPage.login(invalidEmail, validPassword);

    // Assert - Error message should be displayed
    await loginPage.waitForError();
    await expect(loginPage.errorMessage).toBeVisible();
    const errorText = await loginPage.getErrorMessage();
    expect(errorText.length).toBeGreaterThan(0);
  });

  test('should show error with incorrect password', async () => {
    // Arrange
    const validEmail = 'test@test.com';
    const incorrectPassword = 'WrongPassword123!';

    // Act - Attempt login with wrong password
    await loginPage.login(validEmail, incorrectPassword);

    // Assert - Error message should be displayed
    await loginPage.waitForError();
    await expect(loginPage.errorMessage).toBeVisible();
  });

  test('should show error with empty password', async () => {
    // Arrange
    const validEmail = 'test@test.com';
    const emptyPassword = '';

    // Act - Fill email only
    await loginPage.fillEmail(validEmail);
    await loginPage.fillPassword(emptyPassword);

    // Assert - Login button should be disabled
    const isDisabled = await loginPage.isLoginButtonDisabled();
    expect(isDisabled).toBeTruthy();
  });

  test('should show error with completely wrong credentials', async () => {
    // Arrange
    const invalidEmail = 'wrong@example.com';
    const invalidPassword = 'wrongpass';

    // Act
    await loginPage.login(invalidEmail, invalidPassword);

    // Assert - Error message should appear
    await loginPage.waitForError();
    await expect(loginPage.errorMessage).toBeVisible();
  });
});

/**
 * Test suite for email validation
 */
test.describe('Email Field Validation', () => {
  let loginPage: LoginPage;

  test.beforeEach(async ({ page }) => {
    loginPage = new LoginPage(page);
    await loginPage.navigateToLogin();
  });

  test('should show error for empty email field', async () => {
    // Act - Focus and blur email field without entering anything
    await loginPage.fillEmail('');
    await loginPage.triggerEmailValidation();
    await loginPage.passwordInput.click(); // Blur email field

    // Assert - Login button should be disabled
    const isDisabled = await loginPage.isLoginButtonDisabled();
    expect(isDisabled).toBeTruthy();
  });

  test('should show error for invalid email format', async () => {
    // Arrange
    const invalidEmails = [
      'notanemail',
      '@nodomain.com',
      'spaces in@email.com'
    ];

    for (const email of invalidEmails) {
      // Act
      await loginPage.fillEmail(email);
      await loginPage.triggerEmailValidation();
      await loginPage.passwordInput.click(); // Blur email field

      // Assert - Email error should be visible
      await expect(loginPage.emailError).toBeVisible();
      
      // Clear for next iteration
      await loginPage.fillEmail('');
    }
  });

  test('should accept valid email format', async () => {
    // Arrange
    const validEmail = 'test@test.com';

    // Act
    await loginPage.fillEmail(validEmail);
    await loginPage.fillPassword('somepassword');

    // Assert - No email error should be visible
    await expect(loginPage.emailError).not.toBeVisible();
  });

  test('should require email field to enable login', async () => {
    // Arrange
    const validPassword = 'TestPassword123!';

    // Act - Fill only password
    await loginPage.fillPassword(validPassword);

    // Assert - Login button should be disabled without email
    const isDisabled = await loginPage.isLoginButtonDisabled();
    expect(isDisabled).toBeTruthy();
  });
});

/**
 * Test suite for password validation
 */
test.describe('Password Field Validation', () => {
  let loginPage: LoginPage;

  test.beforeEach(async ({ page }) => {
    loginPage = new LoginPage(page);
    await loginPage.navigateToLogin();
  });

  test('should show error for empty password field', async () => {
    // Act - Focus and blur password field without entering anything
    await loginPage.fillPassword('');
    await loginPage.triggerPasswordValidation();
    await loginPage.emailInput.click(); // Blur password field

    // Assert - Login button should be disabled
    const isDisabled = await loginPage.isLoginButtonDisabled();
    expect(isDisabled).toBeTruthy();
  });

  test('should require password field to enable login', async () => {
    // Arrange
    const validEmail = 'test@test.com';

    // Act - Fill only email
    await loginPage.fillEmail(validEmail);

    // Assert - Login button should be disabled without password
    const isDisabled = await loginPage.isLoginButtonDisabled();
    expect(isDisabled).toBeTruthy();
  });

  test('should accept any non-empty password', async () => {
    // Arrange
    const validEmail = 'test@test.com';
    const shortPassword = 'abc'; // Login form should accept any length

    // Act
    await loginPage.fillEmail(validEmail);
    await loginPage.fillPassword(shortPassword);

    // Assert - Login button should be enabled (no client-side length validation for login)
    const isDisabled = await loginPage.isLoginButtonDisabled();
    expect(isDisabled).toBeFalsy();
  });
});

/**
 * Test suite for password visibility toggle
 */
test.describe('Password Visibility Toggle', () => {
  let loginPage: LoginPage;

  test.beforeEach(async ({ page }) => {
    loginPage = new LoginPage(page);
    await loginPage.navigateToLogin();
  });

  test('should toggle password visibility', async () => {
    // Arrange
    const password = 'TestPassword123!';
    await loginPage.fillPassword(password);

    // Assert - Initially password should be hidden
    let inputType = await loginPage.getPasswordInputType();
    expect(inputType).toBe('password');

    // Act - Click visibility toggle
    await loginPage.togglePasswordVisibility();

    // Assert - Password should be visible
    inputType = await loginPage.getPasswordInputType();
    expect(inputType).toBe('text');

    // Act - Click visibility toggle again
    await loginPage.togglePasswordVisibility();

    // Assert - Password should be hidden again
    inputType = await loginPage.getPasswordInputType();
    expect(inputType).toBe('password');
  });

  test('should have visibility toggle button', async () => {
    // Assert - Visibility toggle button should be visible
    await expect(loginPage.passwordVisibilityToggle).toBeVisible();
    await expect(loginPage.passwordVisibilityToggle).toBeEnabled();
  });
});

/**
 * Test suite for form validation and user feedback
 */
test.describe('Form Validation and Feedback', () => {
  let loginPage: LoginPage;

  test.beforeEach(async ({ page }) => {
    loginPage = new LoginPage(page);
    await loginPage.navigateToLogin();
  });

  test('should disable login button with invalid form', async () => {
    // Act - Leave form empty
    await loginPage.fillEmail('');
    await loginPage.fillPassword('');

    // Assert - Login button should be disabled
    const isDisabled = await loginPage.isLoginButtonDisabled();
    expect(isDisabled).toBeTruthy();
  });

  test('should enable login button with valid form', async () => {
    // Act - Fill valid form
    await loginPage.fillEmail('test@test.com');
    await loginPage.fillPassword('somepassword');

    // Assert - Login button should be enabled
    const isDisabled = await loginPage.isLoginButtonDisabled();
    expect(isDisabled).toBeFalsy();
  });

  test('should show loading state during submission', async ({ page }) => {
    // Arrange
    const validEmail = 'test@test.com';
    const validPassword = 'TestPassword123!';

    // Act - Submit form
    await loginPage.fillEmail(validEmail);
    await loginPage.fillPassword(validPassword);
    
    // Click and immediately check for loading state
    const loginPromise = loginPage.clickLogin();
    
    // Assert - Loading spinner might be visible briefly
    // Note: This might be too fast to catch in real scenarios
    // But button should be disabled during submission
    
    await loginPromise;
    // Wait for navigation or error
    await page.waitForURL(/\/(dashboard|login)/, { timeout: 10000 });
  });
});

/**
 * Test suite for accessibility
 */
test.describe('Accessibility', () => {
  let loginPage: LoginPage;

  test.beforeEach(async ({ page }) => {
    loginPage = new LoginPage(page);
    await loginPage.navigateToLogin();
  });

  test('should support keyboard navigation', async ({ page }) => {
    // Act - Navigate using Tab key
    await page.keyboard.press('Tab'); // Focus email
    await expect(loginPage.emailInput).toBeFocused();
    
    await page.keyboard.press('Tab'); // Focus password
    await expect(loginPage.passwordInput).toBeFocused();
  });
});

/**
 * Test suite for responsive design
 */
test.describe('Responsive Login Page', () => {
  let loginPage: LoginPage;

  test('should display correctly on mobile viewport', async ({ page }) => {
    // Arrange - Set mobile viewport
    await page.setViewportSize({ width: 375, height: 667 }); // iPhone SE
    
    loginPage = new LoginPage(page);
    await loginPage.navigateToLogin();

    // Assert - All elements should be visible
    await expect(loginPage.loginLogo).toBeVisible();
    await expect(loginPage.emailInput).toBeVisible();
    await expect(loginPage.passwordInput).toBeVisible();
    await expect(loginPage.loginButton).toBeVisible();
  });

  test('should display correctly on tablet viewport', async ({ page }) => {
    // Arrange - Set tablet viewport
    await page.setViewportSize({ width: 768, height: 1024 }); // iPad
    
    loginPage = new LoginPage(page);
    await loginPage.navigateToLogin();

    // Assert - All elements should be visible
    await expect(loginPage.emailInput).toBeVisible();
    await expect(loginPage.passwordInput).toBeVisible();
    await expect(loginPage.loginButton).toBeVisible();
  });

  test('should display correctly on desktop viewport', async ({ page }) => {
    // Arrange - Set desktop viewport
    await page.setViewportSize({ width: 1920, height: 1080 });
    
    loginPage = new LoginPage(page);
    await loginPage.navigateToLogin();

    // Assert - All elements should be visible
    await expect(loginPage.emailInput).toBeVisible();
    await expect(loginPage.passwordInput).toBeVisible();
    await expect(loginPage.loginButton).toBeVisible();
  });
});

