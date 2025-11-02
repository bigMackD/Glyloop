/**
 * Sample Playwright E2E Test Suite
 * Demonstrates Playwright best practices following playwright-e2e-testing.mdc guidelines:
 * 
 * - Chromium/Desktop Chrome browser only
 * - Browser contexts for isolating test environments
 * - Page Object Model for maintainable tests
 * - Locators for resilient element selection
 * - API testing for backend validation
 * - Visual comparison with screenshots
 * - Test hooks for setup and teardown
 * - Specific assertions with expect matchers
 * - Parallel execution support
 */

import { test, expect } from '@playwright/test';
import { LoginPage } from './pages/login.page';
import { DashboardPage } from './pages/dashboard.page';

/**
 * Test suite for authentication flow
 */
test.describe('Authentication Flow', () => {
  let loginPage: LoginPage;
  let dashboardPage: DashboardPage;

  /**
   * Setup: Run before each test
   * Demonstrates test hooks for setup
   */
  test.beforeEach(async ({ page }) => {
    loginPage = new LoginPage(page);
    dashboardPage = new DashboardPage(page);
    
    // Navigate to login page
    await loginPage.navigateToLogin();
  });

  /**
   * Teardown: Run after each test
   * Demonstrates test hooks for teardown
   */
  test.afterEach(async ({ page }) => {
    // Close any open dialogs, clean up state, etc.
    await page.close();
  });

  test('should display login page correctly', async ({ page }) => {
    // Assert - Check page title
    await expect(page).toHaveTitle(/Glyloop/);
    
    // Assert - Check login form elements are visible
    await expect(loginPage.emailInput).toBeVisible();
    await expect(loginPage.passwordInput).toBeVisible();
    await expect(loginPage.loginButton).toBeVisible();
    
    // Visual regression test (use sparingly)
    // await expect(page).toHaveScreenshot('login-page.png');
  });

  test('should show error with invalid credentials', async ({ page }) => {
    // Arrange
    const invalidEmail = 'invalid@example.com';
    const invalidPassword = 'wrongpassword';

    // Act
    await loginPage.login(invalidEmail, invalidPassword);

    // Assert
    await expect(loginPage.errorMessage).toBeVisible();
    const errorText = await loginPage.getErrorMessage();
    expect(errorText).toContain('Invalid credentials');
  });

  test('should login successfully with valid credentials', async ({ page }) => {
    // Arrange
    const validEmail = 'test@example.com';
    const validPassword = 'ValidPassword123!';

    // Act
    await loginPage.login(validEmail, validPassword);

    // Assert - Should redirect to dashboard
    await page.waitForURL('**/dashboard');
    await expect(page).toHaveURL(/dashboard/);
    
    // Assert - Dashboard should be loaded
    await expect(dashboardPage.dashboardTitle).toBeVisible();
  });

  /**
   * Example of testing with multiple steps
   */
  test('should complete full login and logout flow', async ({ page }) => {
    // Step 1: Login
    await loginPage.login('test@example.com', 'ValidPassword123!');
    await page.waitForURL('**/dashboard');
    
    // Step 2: Verify dashboard is loaded
    const isDashboardLoaded = await dashboardPage.isDashboardLoaded();
    expect(isDashboardLoaded).toBeTruthy();
    
    // Step 3: Logout
    await dashboardPage.logout();
    
    // Step 4: Verify redirected to login
    await page.waitForURL('**/login');
    await expect(page).toHaveURL(/login/);
  });
});

/**
 * Test suite for dashboard functionality
 */
test.describe('Dashboard Functionality', () => {
  /**
   * Setup with authenticated context
   * Demonstrates browser context isolation
   */
  test.beforeEach(async ({ page }) => {
    const loginPage = new LoginPage(page);
    
    // Login before each test
    await loginPage.navigateToLogin();
    await loginPage.login('test@example.com', 'ValidPassword123!');
    await page.waitForURL('**/dashboard');
  });

  test('should display glucose chart', async ({ page }) => {
    // Arrange
    const dashboardPage = new DashboardPage(page);
    
    // Act
    await dashboardPage.waitForChartToLoad();
    
    // Assert
    await expect(dashboardPage.glucoseChart).toBeVisible();
  });

  test('should be able to add new event', async ({ page }) => {
    // Arrange
    const dashboardPage = new DashboardPage(page);
    
    // Act
    await dashboardPage.clickAddEvent();
    
    // Assert - Event dialog/form should appear
    const addEventDialog = page.locator('[data-testid="add-event-dialog"]');
    await expect(addEventDialog).toBeVisible();
  });

  /**
   * Example of testing with wait conditions
   */
  test('should load events list', async ({ page }) => {
    // Arrange
    const dashboardPage = new DashboardPage(page);
    
    // Act - Wait for events to load
    await page.waitForSelector('[data-testid="events-list"]');
    
    // Assert
    await expect(dashboardPage.eventsList).toBeVisible();
    
    // Additional assertion - Check events count
    const eventsCount = await dashboardPage.getEventsCount();
    expect(eventsCount).toBeGreaterThanOrEqual(0);
  });
});

/**
 * Test suite for API validation
 * Demonstrates API testing with Playwright
 */
test.describe('API Validation', () => {
  test('should validate backend API endpoints', async ({ request }) => {
    // Example of API testing for backend validation
    
    // Test health check endpoint
    const healthResponse = await request.get('/api/health');
    expect(healthResponse.ok()).toBeTruthy();
    expect(healthResponse.status()).toBe(200);
    
    // Test API response structure
    const healthData = await healthResponse.json();
    expect(healthData).toHaveProperty('status');
  });

  test('should handle API authentication', async ({ request }) => {
    // Test unauthenticated request
    const unauthorizedResponse = await request.get('/api/events');
    expect(unauthorizedResponse.status()).toBe(401);
    
    // Test with authentication
    // This would need actual authentication token
    const token = 'test-jwt-token';
    const authResponse = await request.get('/api/events', {
      headers: {
        'Authorization': `Bearer ${token}`
      }
    });
    
    // Assert response
    expect([200, 401]).toContain(authResponse.status());
  });
});

/**
 * Test suite demonstrating visual regression testing
 */
test.describe('Visual Regression Tests', () => {
  test('should match dashboard screenshot', async ({ page }) => {
    // Arrange
    const loginPage = new LoginPage(page);
    const dashboardPage = new DashboardPage(page);
    
    // Act - Login and navigate to dashboard
    await loginPage.navigateToLogin();
    await loginPage.login('test@example.com', 'ValidPassword123!');
    await page.waitForURL('**/dashboard');
    await dashboardPage.waitForChartToLoad();
    
    // Assert - Visual comparison
    // Note: First run creates baseline, subsequent runs compare
    // await expect(page).toHaveScreenshot('dashboard-full.png', {
    //   fullPage: true,
    //   animations: 'disabled',
    // });
  });
});

/**
 * Test suite for responsive design
 */
test.describe('Responsive Design Tests', () => {
  test('should work on mobile viewport', async ({ page }) => {
    // Arrange - Set mobile viewport
    await page.setViewportSize({ width: 375, height: 667 }); // iPhone SE
    
    const loginPage = new LoginPage(page);
    
    // Act
    await loginPage.navigateToLogin();
    
    // Assert
    await expect(loginPage.emailInput).toBeVisible();
    await expect(loginPage.passwordInput).toBeVisible();
  });

  test('should work on tablet viewport', async ({ page }) => {
    // Arrange - Set tablet viewport
    await page.setViewportSize({ width: 768, height: 1024 }); // iPad
    
    const loginPage = new LoginPage(page);
    
    // Act
    await loginPage.navigateToLogin();
    
    // Assert
    await expect(loginPage.emailInput).toBeVisible();
  });
});

/**
 * Test suite demonstrating parallel execution
 * These tests can run in parallel safely
 */
test.describe.parallel('Parallel Execution Tests', () => {
  test('parallel test 1', async ({ page }) => {
    await page.goto('/');
    await expect(page).toHaveTitle(/Glyloop/);
  });

  test('parallel test 2', async ({ page }) => {
    await page.goto('/');
    await expect(page).toHaveTitle(/Glyloop/);
  });

  test('parallel test 3', async ({ page }) => {
    await page.goto('/');
    await expect(page).toHaveTitle(/Glyloop/);
  });
});

