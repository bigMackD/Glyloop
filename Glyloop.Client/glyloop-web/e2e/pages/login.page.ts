import { Page, Locator } from '@playwright/test';

/**
 * Page Object Model for Login Page
 * Encapsulates interactions with the login page
 */
export class LoginPage {
  readonly page: Page;
  readonly emailInput: Locator;
  readonly passwordInput: Locator;
  readonly passwordVisibilityToggle: Locator;
  readonly loginButton: Locator;
  readonly errorMessage: Locator;
  readonly emailError: Locator;
  readonly passwordError: Locator;
  readonly registerLink: Locator;
  readonly loginForm: Locator;
  readonly loginLogo: Locator;
  readonly loginTitle: Locator;
  readonly loadingSpinner: Locator;

  constructor(page: Page) {
    this.page = page;
    this.emailInput = page.locator('[data-testid="login-email-input"]');
    this.passwordInput = page.locator('[data-testid="login-password-input"]');
    this.passwordVisibilityToggle = page.locator('[data-testid="login-password-visibility-toggle"]');
    this.loginButton = page.locator('[data-testid="login-submit-button"]');
    this.errorMessage = page.locator('[data-testid="login-error-message"]');
    this.emailError = page.locator('[data-testid="login-email-error"]');
    this.passwordError = page.locator('[data-testid="login-password-error"]');
    this.registerLink = page.locator('[data-testid="register-link"]');
    this.loginForm = page.locator('[data-testid="login-form"]');
    this.loginLogo = page.locator('[data-testid="login-logo"]');
    this.loginTitle = page.locator('[data-testid="login-title"]');
    this.loadingSpinner = page.locator('[data-testid="login-submit-loading"]');
  }

  /**
   * Navigate to the login page
   */
  async navigateToLogin(): Promise<void> {
    await this.page.goto('/login');
  }

  /**
   * Fill in the login form and submit
   * @param email - User email
   * @param password - User password
   */
  async login(email: string, password: string): Promise<void> {
    await this.emailInput.fill(email);
    await this.passwordInput.fill(password);
    await this.loginButton.click();
  }

  /**
   * Fill email field
   * @param email - User email
   */
  async fillEmail(email: string): Promise<void> {
    await this.emailInput.fill(email);
  }

  /**
   * Fill password field
   * @param password - User password
   */
  async fillPassword(password: string): Promise<void> {
    await this.passwordInput.fill(password);
  }

  /**
   * Click the login button
   */
  async clickLogin(): Promise<void> {
    await this.loginButton.click();
  }

  /**
   * Toggle password visibility
   */
  async togglePasswordVisibility(): Promise<void> {
    await this.passwordVisibilityToggle.click();
  }

  /**
   * Get the error message text
   */
  async getErrorMessage(): Promise<string> {
    return await this.errorMessage.textContent() || '';
  }

  /**
   * Get the email error message text
   */
  async getEmailErrorMessage(): Promise<string> {
    return await this.emailError.textContent() || '';
  }

  /**
   * Get the password error message text
   */
  async getPasswordErrorMessage(): Promise<string> {
    return await this.passwordError.textContent() || '';
  }

  /**
   * Check if login button is disabled
   */
  async isLoginButtonDisabled(): Promise<boolean> {
    return await this.loginButton.isDisabled();
  }

  /**
   * Check if the form is displayed
   */
  async isFormDisplayed(): Promise<boolean> {
    return await this.loginForm.isVisible();
  }

  /**
   * Wait for error message to appear
   */
  async waitForError(): Promise<void> {
    await this.errorMessage.waitFor({ state: 'visible' });
  }

  /**
   * Navigate to register page via link
   */
  async clickRegisterLink(): Promise<void> {
    await this.registerLink.click();
  }

  /**
   * Get password input type (to check visibility)
   */
  async getPasswordInputType(): Promise<string | null> {
    return await this.passwordInput.getAttribute('type');
  }

  /**
   * Trigger validation by blurring fields
   */
  async triggerEmailValidation(): Promise<void> {
    await this.emailInput.click();
    await this.emailInput.blur();
  }

  /**
   * Trigger password validation by blurring field
   */
  async triggerPasswordValidation(): Promise<void> {
    await this.passwordInput.click();
    await this.passwordInput.blur();
  }
}
