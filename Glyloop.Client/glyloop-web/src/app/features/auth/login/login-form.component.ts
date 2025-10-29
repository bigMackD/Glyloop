import { Component, ChangeDetectionStrategy, input, output, signal, effect } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';

import { LoginFormModel } from '../../../core/models/auth.types';

@Component({
  selector: 'app-login-form',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatIconModule
  ],
  templateUrl: './login-form.component.html',
  styleUrl: './login-form.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class LoginFormComponent {
  // Inputs
  readonly isSubmitting = input.required<boolean>();
  readonly serverError = input<string | undefined>();
  readonly fieldErrors = input<{ email?: string; password?: string } | undefined>();
  readonly autofocusEmail = input<boolean>(true);

  // Output
  readonly submitted = output<LoginFormModel>();

  // Password visibility toggle
  readonly hidePassword = signal(true);

  // Form controls - No minLength validation for login per plan
  readonly emailControl = new FormControl('', {
    nonNullable: true,
    validators: [Validators.required, Validators.email]
  });

  readonly passwordControl = new FormControl('', {
    nonNullable: true,
    validators: [Validators.required]
  });

  readonly form = new FormGroup({
    email: this.emailControl,
    password: this.passwordControl
  });

  constructor() {
    // React to fieldErrors changes from parent
    effect(() => {
      const errors = this.fieldErrors();
      if (errors?.email) {
        this.emailControl.setErrors({
          ...(this.emailControl.errors ?? {}),
          serverError: errors.email
        });
      }
      if (errors?.password) {
        this.passwordControl.setErrors({
          ...(this.passwordControl.errors ?? {}),
          serverError: errors.password
        });
      }
    });
  }

  togglePasswordVisibility(): void {
    this.hidePassword.set(!this.hidePassword());
  }

  onSubmit(): void {
    // Mark all fields as touched to show validation errors
    this.form.markAllAsTouched();

    if (this.form.invalid || this.isSubmitting()) {
      // Focus first invalid control
      this.focusFirstInvalidControl();
      return;
    }

    // Emit form value
    const value = this.form.getRawValue();
    this.submitted.emit(value);
  }

  private focusFirstInvalidControl(): void {
    if (this.emailControl.invalid) {
      document.getElementById('login-email')?.focus();
    } else if (this.passwordControl.invalid) {
      document.getElementById('login-password')?.focus();
    }
  }

  // Helper methods for template
  getEmailErrorMessage(): string {
    if (this.emailControl.hasError('required')) {
      return $localize`:@@login.form.email.error.required:Email is required`;
    }
    if (this.emailControl.hasError('email')) {
      return $localize`:@@login.form.email.error.invalid:Please enter a valid email address`;
    }
    if (this.emailControl.hasError('serverError')) {
      return this.emailControl.getError('serverError');
    }
    return '';
  }

  getPasswordErrorMessage(): string {
    if (this.passwordControl.hasError('required')) {
      return $localize`:@@login.form.password.error.required:Password is required`;
    }
    if (this.passwordControl.hasError('serverError')) {
      return this.passwordControl.getError('serverError');
    }
    return '';
  }

  getPasswordVisibilityLabel(): string {
    return this.hidePassword()
      ? $localize`:@@login.form.password.showLabel:Show password`
      : $localize`:@@login.form.password.hideLabel:Hide password`;
  }
}
