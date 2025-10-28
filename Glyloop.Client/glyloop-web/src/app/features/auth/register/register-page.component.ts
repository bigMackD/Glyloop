import { Component, ChangeDetectionStrategy, signal, inject } from '@angular/core';
import { Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { finalize } from 'rxjs';

import { RegisterFormComponent } from './register-form.component';
import { AuthApiService } from '../../../core/services/auth-api.service';
import { RegisterFormModel } from '../../../core/models/auth.types';
import { ProblemDetails } from '../../../core/models/common.types';

@Component({
  selector: 'app-register-page',
  standalone: true,
  imports: [RegisterFormComponent],
  templateUrl: './register-page.component.html',
  styleUrl: './register-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class RegisterPageComponent {
  private readonly authApi = inject(AuthApiService);
  private readonly router = inject(Router);

  // State signals
  readonly isSubmitting = signal(false);
  readonly serverError = signal<string | undefined>(undefined);
  readonly emailTaken = signal(false);
  readonly success = signal(false);

  onSubmit(model: RegisterFormModel): void {
    this.isSubmitting.set(true);
    this.serverError.set(undefined);
    this.emailTaken.set(false);

    this.authApi.register(model)
      .pipe(finalize(() => this.isSubmitting.set(false)))
      .subscribe({
        next: () => {
          this.success.set(true);
          // Navigate to login with success flag
          this.router.navigate(['/login'], { queryParams: { registered: true } });
        },
        error: (err: HttpErrorResponse) => {
          const problem = err.error as ProblemDetails | undefined;
          
          if (err.status === 409) {
            // Email already exists
            this.emailTaken.set(true);
          } else if (problem?.detail) {
            // Show problem detail from server
            this.serverError.set(problem.detail);
          } else if (err.status === 0) {
            // Network error
            this.serverError.set($localize`:@@register.error.network:Unable to connect to the server. Please check your connection and try again.`);
          } else {
            // Generic fallback
            this.serverError.set($localize`:@@register.error.generic:Registration failed. Please try again.`);
          }
        }
      });
  }

  onGoToLogin(): void {
    this.router.navigate(['/login'], { queryParams: { registered: true } });
  }
}

