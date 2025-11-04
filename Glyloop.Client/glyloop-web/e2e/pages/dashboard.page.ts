/**
 * Dashboard Page Object Model
 * Demonstrates interaction with complex UI components
 */

import { Page, Locator } from '@playwright/test';
import { BasePage } from './base.page';

export class DashboardPage extends BasePage {
  // Navigation elements
  readonly dashboardTitle: Locator;
  readonly userMenu: Locator;
  readonly logoutButton: Locator;

  // Chart elements
  readonly glucoseChart: Locator;
  readonly chartLoadingIndicator: Locator;

  // Event elements
  readonly addEventButton: Locator;
  readonly eventsList: Locator;

  constructor(page: Page) {
    super(page);
    
    // Initialize locators
    this.dashboardTitle = page.locator('h1', { hasText: 'Dashboard' });
    this.userMenu = page.locator('[data-testid="user-menu"]');
    this.logoutButton = page.locator('[data-testid="logout-button"]');
    
    this.glucoseChart = page.locator('[data-testid="glucose-chart"]');
    this.chartLoadingIndicator = page.locator('[data-testid="chart-loading"]');
    
    this.addEventButton = page.locator('button', { hasText: 'Add Event' });
    this.eventsList = page.locator('[data-testid="events-list"]');
  }

  /**
   * Navigate to dashboard
   */
  async navigateToDashboard(): Promise<void> {
    await this.goto('/dashboard');
    await this.waitForPageLoad();
  }

  /**
   * Wait for chart to load
   */
  async waitForChartToLoad(): Promise<void> {
    await this.chartLoadingIndicator.waitFor({ state: 'hidden', timeout: 10000 });
    await this.waitForElement(this.glucoseChart);
  }

  /**
   * Open user menu
   */
  async openUserMenu(): Promise<void> {
    await this.clickElement(this.userMenu);
  }

  /**
   * Logout
   */
  async logout(): Promise<void> {
    await this.openUserMenu();
    await this.clickElement(this.logoutButton);
  }

  /**
   * Click add event button
   */
  async clickAddEvent(): Promise<void> {
    await this.clickElement(this.addEventButton);
  }

  /**
   * Get events count
   */
  async getEventsCount(): Promise<number> {
    const events = await this.eventsList.locator('[data-testid="event-item"]').count();
    return events;
  }

  /**
   * Check if dashboard is loaded
   */
  async isDashboardLoaded(): Promise<boolean> {
    return await this.isVisible(this.dashboardTitle);
  }
}

