import { Component, ChangeDetectionStrategy, signal, inject, OnInit } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { finalize } from 'rxjs';

import { SessionExpiredBannerComponent } from './session-expired-banner.component';
import { LoginFormComponent } from './login-form.component';
import { AuthFooterLinksComponent } from './auth-footer-links.component';
import { AppHeaderComponent } from '../../../core/shell/app-header.component';
import { AuthApiService } from '../../../core/services/auth-api.service';
import { LoginFormModel } from '../../../core/models/auth.types';
import { ProblemDetails } from '../../../core/models/common.types';

@Component({
  selector: 'app-login-page',
  standalone: true,
  imports: [
    SessionExpiredBannerComponent,
    LoginFormComponent,
    AuthFooterLinksComponent,
    AppHeaderComponent
  ],
  templateUrl: './login-page.component.html',
  styleUrl: './login-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class LoginPageComponent implements OnInit {
  private readonly authApi = inject(AuthApiService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  // State signals
  readonly isSubmitting = signal(false);
  readonly serverError = signal<string | undefined>(undefined);
  readonly fieldErrors = signal<{ email?: string; password?: string } | undefined>(undefined);
  readonly showSessionExpired = signal(false);
  readonly showRegistrationSuccess = signal(false);
  private redirectTo = '/dashboard'; // Default redirect target

  // Localized strings
  readonly pageTitle = $localize`:@@login.title:Sign in to your account`;
  readonly pageSubtitle = $localize`:@@login.subtitle:Welcome back to Glyloop`;
  readonly registrationSuccessTitle = $localize`:@@login.registrationSuccess.title:Account created successfully!`;
  readonly registrationSuccessMessage = $localize`:@@login.registrationSuccess.message:Please sign in with your new account.`;

  ngOnInit(): void {
    // Read query params
    const queryParams = this.route.snapshot.queryParams;

    if (queryParams['reason'] === 'sessionExpired') {
      this.showSessionExpired.set(true);
    }

    if (queryParams['registered'] === 'true') {
      this.showRegistrationSuccess.set(true);
    }

    if (queryParams['redirect']) {
      this.redirectTo = queryParams['redirect'];
    }
  }

  onSubmit(model: LoginFormModel): void {
    this.isSubmitting.set(true);
    this.serverError.set(undefined);
    this.fieldErrors.set(undefined);

    this.authApi.login(model)
      .pipe(finalize(() => this.isSubmitting.set(false)))
      .subscribe({
        next: () => {
          // Login successful - navigate to redirect target
          console.log('Login successful - navigating to redirect target:', this.redirectTo);
          this.router.navigate([this.redirectTo]);
        },
        error: (err: HttpErrorResponse) => {
          this.handleLoginError(err);
        }
      });
  }

  private handleLoginError(err: HttpErrorResponse): void {
    const problem = err.error as ProblemDetails | undefined;

    if (err.status === 401) {
      // Authentication failed
      if (problem?.title?.includes('Account Locked')) {
        // Account lockout
        this.serverError.set($localize`:@@login.error.accountLocked:Your account has been locked due to multiple failed login attempts. Please try again later or contact support.`);
      } else {
        // Generic auth failure - keep email, suggest checking credentials
        this.serverError.set($localize`:@@login.error.invalidCredentials:Invalid email or password. Please check your credentials and try again.`);
      }
    } else if (err.status === 400) {
      // Bad request - map to field errors if available
      if (problem?.detail) {
        this.serverError.set(problem.detail);
      } else {
        this.serverError.set($localize`:@@login.error.badRequest:Invalid request. Please check your input and try again.`);
      }
    } else if (err.status === 0) {
      // Network error
      this.serverError.set($localize`:@@login.error.network:Cannot reach server. Check connection and try again.`);
    } else {
      // Generic fallback
      this.serverError.set($localize`:@@login.error.generic:Login failed. Please try again.`);
    }
  }
}
