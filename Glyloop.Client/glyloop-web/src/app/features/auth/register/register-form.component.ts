import { Component, ChangeDetectionStrategy, input, output, signal, effect } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

import { RegisterFormModel } from '../../../core/models/auth.types';

@Component({
  selector: 'app-register-form',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule
  ],
  templateUrl: './register-form.component.html',
  styleUrl: './register-form.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class RegisterFormComponent {
  // Inputs
  readonly isSubmitting = input.required<boolean>();
  readonly serverError = input<string | undefined>();
  readonly emailTaken = input<boolean>(false);

  // Output
  readonly submitted = output<RegisterFormModel>();

  // Password visibility toggle
  readonly hidePassword = signal(true);

  // Form controls
  readonly emailControl = new FormControl('', {
    nonNullable: true,
    validators: [Validators.required, Validators.email]
  });

  readonly passwordControl = new FormControl('', {
    nonNullable: true,
    validators: [Validators.required, Validators.minLength(12)]
  });

  readonly form = new FormGroup({
    email: this.emailControl,
    password: this.passwordControl
  });

  constructor() {
    // React to emailTaken changes from parent
    effect(() => {
      if (this.emailTaken()) {
        this.setEmailTakenError(true);
      } else {
        this.setEmailTakenError(false);
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

  private setEmailTakenError(taken: boolean): void {
    if (taken) {
      this.emailControl.setErrors({
        ...(this.emailControl.errors ?? {}),
        emailTaken: true
      });
    } else if (this.emailControl.hasError('emailTaken')) {
      // Remove emailTaken error while keeping other errors
      // eslint-disable-next-line @typescript-eslint/no-unused-vars
      const { emailTaken, ...rest } = this.emailControl.errors ?? {};
      this.emailControl.setErrors(Object.keys(rest).length ? rest : null);
    }
  }

  private focusFirstInvalidControl(): void {
    if (this.emailControl.invalid) {
      document.getElementById('register-email')?.focus();
    } else if (this.passwordControl.invalid) {
      document.getElementById('register-password')?.focus();
    }
  }

  // Helper methods for template
  getEmailErrorMessage(): string {
    if (this.emailControl.hasError('required')) {
      return $localize`:@@register.form.email.error.required:Email is required`;
    }
    if (this.emailControl.hasError('email')) {
      return $localize`:@@register.form.email.error.invalid:Please enter a valid email address`;
    }
    if (this.emailControl.hasError('emailTaken')) {
      return $localize`:@@register.form.email.error.taken:Email is already registered. Try logging in.`;
    }
    return '';
  }

  getPasswordErrorMessage(): string {
    if (this.passwordControl.hasError('required')) {
      return $localize`:@@register.form.password.error.required:Password is required`;
    }
    if (this.passwordControl.hasError('minlength')) {
      const minLength = this.passwordControl.getError('minlength')?.requiredLength ?? 12;
      return $localize`:@@register.form.password.error.minlength:Password must be at least ${minLength}:minLength: characters`;
    }
    return '';
  }

  getPasswordVisibilityLabel(): string {
    return this.hidePassword() 
      ? $localize`:@@register.form.password.showLabel:Show password`
      : $localize`:@@register.form.password.hideLabel:Hide password`;
  }
}

