import { Page, Locator } from '@playwright/test';

/**
 * Page Object Model for Dashboard Page
 * Encapsulates interactions with the dashboard page
 */
export class DashboardPage {
  readonly page: Page;
  readonly dashboardTitle: Locator;
  readonly addEventButton: Locator;
  readonly glucoseChart: Locator;
  readonly eventsList: Locator;
  readonly userMenuButton: Locator;
  readonly logoutButton: Locator;
  readonly addEventDialog: Locator;
  readonly addEventCloseButton: Locator;
  readonly addEventSubmitButton: Locator;
  readonly addEventCancelButton: Locator;

  // Event tabs
  readonly foodTab: Locator;
  readonly insulinTab: Locator;
  readonly exerciseTab: Locator;
  readonly noteTab: Locator;

  // Food form fields
  readonly foodCarbsInput: Locator;

  // Insulin form fields
  readonly insulinTypeSelect: Locator;
  readonly insulinUnitsInput: Locator;

  constructor(page: Page) {
    this.page = page;
    this.dashboardTitle = page.locator('[data-testid="dashboard-title"]');
    this.addEventButton = page.locator('[data-testid="add-event-button"]');
    this.glucoseChart = page.locator('[data-testid="glucose-chart"]');
    this.eventsList = page.locator('[data-testid="events-list"]');
    this.userMenuButton = page.locator('[data-testid="user-menu-button"]');
    this.logoutButton = page.locator('[data-testid="user-menu-logout"]');
    this.addEventDialog = page.locator('[data-testid="add-event-dialog"]');
    this.addEventCloseButton = page.locator('[data-testid="add-event-close-button"]');
    this.addEventSubmitButton = page.locator('[data-testid="add-event-submit-button"]');
    this.addEventCancelButton = page.locator('[data-testid="add-event-cancel-button"]');

    // Event tabs
    this.foodTab = page.locator('[data-testid="food-tab"]');
    this.insulinTab = page.locator('[data-testid="insulin-tab"]');
    this.exerciseTab = page.locator('[data-testid="exercise-tab"]');
    this.noteTab = page.locator('[data-testid="note-tab"]');

    // Food form fields
    this.foodCarbsInput = page.locator('[data-testid="food-carbs-input"]');

    // Insulin form fields
    this.insulinTypeSelect = page.locator('[data-testid="insulin-type-select"]');
    this.insulinUnitsInput = page.locator('[data-testid="insulin-units-input"]');
  }

  /**
   * Navigate to the dashboard page
   */
  async navigateToDashboard(): Promise<void> {
    await this.page.goto('/dashboard');
  }

  /**
   * Click the add event button
   */
  async clickAddEvent(): Promise<void> {
    await this.addEventButton.click();
  }

  /**
   * Wait for the chart to load
   */
  async waitForChartToLoad(): Promise<void> {
    await this.glucoseChart.waitFor({ state: 'visible' });
  }

  /**
   * Check if dashboard is loaded
   */
  async isDashboardLoaded(): Promise<boolean> {
    const isTitleVisible = await this.dashboardTitle.isVisible();
    const isChartVisible = await this.glucoseChart.isVisible();
    return isTitleVisible && isChartVisible;
  }

  /**
   * Open the user menu and logout
   */
  async logout(): Promise<void> {
    await this.userMenuButton.click();
    await this.logoutButton.click();
  }

  /**
   * Get the count of events in the list
   */
  async getEventsCount(): Promise<number> {
    // Assuming events have a specific selector within the events-list
    const events = this.eventsList.locator('[class*="event-item"]');
    return await events.count();
  }

  /**
   * Wait for the add event dialog to open
   */
  async waitForAddEventDialog(): Promise<void> {
    await this.addEventDialog.waitFor({ state: 'visible' });
  }

  /**
   * Close the add event dialog
   */
  async closeAddEventDialog(): Promise<void> {
    await this.addEventCloseButton.click();
  }

  /**
   * Cancel the add event dialog
   */
  async cancelAddEventDialog(): Promise<void> {
    await this.addEventCancelButton.click();
  }

  /**
   * Fill food event form
   * @param carbs - Carbohydrates in grams
   */
  async fillFoodEvent(carbs: number): Promise<void> {
    await this.foodTab.click();
    await this.foodCarbsInput.fill(carbs.toString());
  }

  /**
   * Fill insulin event form
   * @param type - Insulin type
   * @param units - Insulin units
   */
  async fillInsulinEvent(type: string, units: number): Promise<void> {
    await this.insulinTab.click();
    await this.insulinTypeSelect.click();
    await this.page.locator(`mat-option:has-text("${type}")`).click();
    await this.insulinUnitsInput.fill(units.toString());
  }

  /**
   * Submit the add event form
   */
  async submitAddEvent(): Promise<void> {
    await this.addEventSubmitButton.click();
  }

  /**
   * Check if add event dialog is visible
   */
  async isAddEventDialogVisible(): Promise<boolean> {
    return await this.addEventDialog.isVisible();
  }
}
