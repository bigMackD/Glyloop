/**
 * Base Page Object Model class
 * Provides common functionality for all page objects
 * Following Playwright best practices with locators and assertions
 */

import { Page, Locator } from '@playwright/test';

export abstract class BasePage {
  constructor(protected page: Page) {}

  /**
   * Navigate to a specific URL
   */
  async goto(path: string): Promise<void> {
    await this.page.goto(path);
  }

  /**
   * Get the current page title
   */
  async getTitle(): Promise<string> {
    return await this.page.title();
  }

  /**
   * Wait for an element to be visible
   */
  async waitForElement(locator: Locator): Promise<void> {
    await locator.waitFor({ state: 'visible' });
  }

  /**
   * Click an element with automatic wait
   */
  async clickElement(locator: Locator): Promise<void> {
    await locator.click();
  }

  /**
   * Fill input with text
   */
  async fillInput(locator: Locator, text: string): Promise<void> {
    await locator.fill(text);
  }

  /**
   * Get text content of an element
   */
  async getTextContent(locator: Locator): Promise<string | null> {
    return await locator.textContent();
  }

  /**
   * Check if element is visible
   */
  async isVisible(locator: Locator): Promise<boolean> {
    return await locator.isVisible();
  }

  /**
   * Wait for page to be loaded
   */
  async waitForPageLoad(): Promise<void> {
    await this.page.waitForLoadState('networkidle');
  }

  /**
   * Take a screenshot
   */
  async takeScreenshot(name: string): Promise<void> {
    await this.page.screenshot({ path: `test-results/${name}.png`, fullPage: true });
  }
}

