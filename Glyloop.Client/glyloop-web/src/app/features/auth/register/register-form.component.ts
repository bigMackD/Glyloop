import { Component, ChangeDetectionStrategy, input, output, signal, effect } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';

import { RegisterFormModel } from '../../../core/models/auth.types';

// Custom validator to check if passwords match
function passwordsMatchValidator(control: AbstractControl): ValidationErrors | null {
  const password = control.get('password');
  const confirmPassword = control.get('confirmPassword');

  if (!password || !confirmPassword) {
    return null;
  }

  if (confirmPassword.value === '') {
    return null; // Let required validator handle empty field
  }

  return password.value === confirmPassword.value ? null : { passwordsMismatch: true };
}

@Component({
  selector: 'app-register-form',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
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

  // Password visibility toggles
  readonly hidePassword = signal(true);
  readonly hideConfirmPassword = signal(true);

  // Form controls
  readonly emailControl = new FormControl('', {
    nonNullable: true,
    validators: [Validators.required, Validators.email]
  });

  readonly passwordControl = new FormControl('', {
    nonNullable: true,
    validators: [Validators.required, Validators.minLength(12)]
  });

  readonly confirmPasswordControl = new FormControl('', {
    nonNullable: true,
    validators: [Validators.required]
  });

  readonly form = new FormGroup({
    email: this.emailControl,
    password: this.passwordControl,
    confirmPassword: this.confirmPasswordControl
  }, { validators: passwordsMatchValidator });

  constructor() {
    effect(() => {
      if (this.emailTaken()) {
        this.setEmailTakenError(true);
      } else {
        this.setEmailTakenError(false);
      }
    });

    this.passwordControl.valueChanges.subscribe(() => {
      this.form.updateValueAndValidity();
      this.updatePasswordMismatchError();
    });

    this.confirmPasswordControl.valueChanges.subscribe(() => {
      this.form.updateValueAndValidity();
      this.updatePasswordMismatchError();
    });
  }

  togglePasswordVisibility(): void {
    this.hidePassword.set(!this.hidePassword());
  }

  toggleConfirmPasswordVisibility(): void {
    this.hideConfirmPassword.set(!this.hideConfirmPassword());
  }

  onSubmit(): void {
    this.form.markAllAsTouched();

    if (this.form.invalid || this.isSubmitting()) {
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

  private updatePasswordMismatchError(): void {
    if (this.form.hasError('passwordsMismatch') && this.confirmPasswordControl.value !== '') {
      this.confirmPasswordControl.setErrors({
        ...(this.confirmPasswordControl.errors ?? {}),
        passwordsMismatch: true
      });
    } else if (this.confirmPasswordControl.hasError('passwordsMismatch')) {
      // eslint-disable-next-line @typescript-eslint/no-unused-vars
      const { passwordsMismatch, ...rest } = this.confirmPasswordControl.errors ?? {};
      this.confirmPasswordControl.setErrors(Object.keys(rest).length ? rest : null);
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

  getConfirmPasswordErrorMessage(): string {
    if (this.confirmPasswordControl.hasError('required')) {
      return $localize`:@@register.form.confirmPassword.error.required:Please confirm your password`;
    }
    if (this.confirmPasswordControl.hasError('passwordsMismatch')) {
      return $localize`:@@register.form.confirmPassword.error.mismatch:Passwords do not match`;
    }
    return '';
  }

  getConfirmPasswordVisibilityLabel(): string {
    return this.hideConfirmPassword()
      ? $localize`:@@register.form.confirmPassword.showLabel:Show confirm password`
      : $localize`:@@register.form.confirmPassword.hideLabel:Hide confirm password`;
  }
}

