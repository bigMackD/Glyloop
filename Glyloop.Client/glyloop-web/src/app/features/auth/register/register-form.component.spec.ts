/**
 * Unit tests for RegisterFormComponent
 * Tests all key features including:
 * - Reactive form architecture (with edge cases for special characters, long passwords, form reset)
 * - Custom password matching validator
 * - Signal-based state management
 * - Password visibility toggle (with edge cases for rapid toggles)
 * - Dynamic server-side validation
 * - Real-time password mismatch validation (with edge cases for field update order)
 * - Comprehensive error messages
 * - i18n support
 * - Smart error management
 * - Form submission logic
 * - Integration scenarios
 * 
 * Note: Edge cases are integrated alongside relevant feature tests, not separated
 */

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { RegisterFormComponent } from './register-form.component';
import { RegisterFormModel } from '../../../core/models/auth.types';

describe('RegisterFormComponent', () => {
  let component: RegisterFormComponent;
  let fixture: ComponentFixture<RegisterFormComponent>;

  /**
   * Setup: Configure TestBed with all required imports and dependencies
   */
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        RegisterFormComponent,
        ReactiveFormsModule,
        MatFormFieldModule,
        MatInputModule,
        MatIconModule,
        BrowserAnimationsModule
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(RegisterFormComponent);
    component = fixture.componentInstance;
    
    // Set required inputs with default values
    fixture.componentRef.setInput('isSubmitting', false);
    fixture.componentRef.setInput('serverError', undefined);
    fixture.componentRef.setInput('emailTaken', false);
    
    fixture.detectChanges();
  });

  /**
   * Cleanup: Clear mocks after each test
   */
  afterEach(() => {
    jest.clearAllMocks();
  });

  // ============================================================================
  // FEATURE 1: Component Initialization & Reactive Form Architecture
  // ============================================================================
  describe('Component Initialization', () => {
    it('should create the component', () => {
      expect(component).toBeTruthy();
    });

    it('should initialize with empty form controls', () => {
      expect(component.emailControl.value).toBe('');
      expect(component.passwordControl.value).toBe('');
      expect(component.confirmPasswordControl.value).toBe('');
    });

    it('should initialize form as invalid', () => {
      expect(component.form.valid).toBe(false);
    });

    it('should have all required form controls', () => {
      expect(component.form.get('email')).toBeTruthy();
      expect(component.form.get('password')).toBeTruthy();
      expect(component.form.get('confirmPassword')).toBeTruthy();
    });

    it('should have correct validators on email control', () => {
      const emailControl = component.emailControl;
      
      // Test required validator
      emailControl.setValue('');
      expect(emailControl.hasError('required')).toBe(true);
      
      // Test email validator
      emailControl.setValue('invalid-email');
      expect(emailControl.hasError('email')).toBe(true);
      
      // Valid email
      emailControl.setValue('test@example.com');
      expect(emailControl.valid).toBe(true);
    });

    it('should handle special characters in email', () => {
      component.emailControl.setValue('user+test@example.com');
      expect(component.emailControl.valid).toBe(true);
    });

    it('should have correct validators on password control', () => {
      const passwordControl = component.passwordControl;
      
      // Test required validator
      passwordControl.setValue('');
      expect(passwordControl.hasError('required')).toBe(true);
      
      // Test minLength validator (less than 12 characters)
      passwordControl.setValue('short');
      expect(passwordControl.hasError('minlength')).toBe(true);
      
      // Valid password (12+ characters)
      passwordControl.setValue('validPassword123');
      expect(passwordControl.valid).toBe(true);
    });

    it('should handle password with special characters', () => {
      const specialPassword = 'P@ssw0rd!#$%^&*()';
      component.passwordControl.setValue(specialPassword);
      component.confirmPasswordControl.setValue(specialPassword);
      
      expect(component.passwordControl.valid).toBe(true);
      expect(component.form.hasError('passwordsMismatch')).toBe(false);
    });

    it('should have required validator on confirmPassword control', () => {
      const confirmPasswordControl = component.confirmPasswordControl;
      
      // Test required validator
      confirmPasswordControl.setValue('');
      expect(confirmPasswordControl.hasError('required')).toBe(true);
      
      // With value
      confirmPasswordControl.setValue('somePassword');
      expect(confirmPasswordControl.hasError('required')).toBe(false);
    });

    it('should handle form reset scenario', () => {
      // Fill form
      component.emailControl.setValue('test@example.com');
      component.passwordControl.setValue('validPassword123');
      component.confirmPasswordControl.setValue('validPassword123');
      
      expect(component.form.valid).toBe(true);
      
      // Reset form
      component.form.reset();
      
      expect(component.emailControl.value).toBe('');
      expect(component.passwordControl.value).toBe('');
      expect(component.confirmPasswordControl.value).toBe('');
      expect(component.form.valid).toBe(false);
    });
  });

  // ============================================================================
  // FEATURE 2: Custom Password Matching Validator
  // ============================================================================
  describe('Custom Password Matching Validator', () => {
    it('should validate matching passwords', () => {
      const password = 'validPassword123';
      
      component.passwordControl.setValue(password);
      component.confirmPasswordControl.setValue(password);
      component.form.updateValueAndValidity();
      
      expect(component.form.hasError('passwordsMismatch')).toBe(false);
    });

    it('should invalidate non-matching passwords', () => {
      component.passwordControl.setValue('password123');
      component.confirmPasswordControl.setValue('differentPassword456');
      component.form.updateValueAndValidity();
      
      expect(component.form.hasError('passwordsMismatch')).toBe(true);
    });

    it('should not show mismatch error when confirmPassword is empty', () => {
      component.passwordControl.setValue('validPassword123');
      component.confirmPasswordControl.setValue('');
      component.form.updateValueAndValidity();
      
      // Should have required error, not mismatch error
      expect(component.form.hasError('passwordsMismatch')).toBe(false);
    });

    it('should update validation when password changes', () => {
      // Set matching passwords
      component.passwordControl.setValue('password123');
      component.confirmPasswordControl.setValue('password123');
      component.form.updateValueAndValidity();
      expect(component.form.hasError('passwordsMismatch')).toBe(false);
      
      // Change password to make them not match
      component.passwordControl.setValue('differentPassword456');
      
      // Validation should update
      expect(component.form.hasError('passwordsMismatch')).toBe(true);
    });

    it('should update validation when confirmPassword changes', () => {
      // Set non-matching passwords
      component.passwordControl.setValue('password123');
      component.confirmPasswordControl.setValue('different');
      component.form.updateValueAndValidity();
      expect(component.form.hasError('passwordsMismatch')).toBe(true);
      
      // Change confirmPassword to match
      component.confirmPasswordControl.setValue('password123');
      
      // Validation should update
      expect(component.form.hasError('passwordsMismatch')).toBe(false);
    });
  });

  // ============================================================================
  // FEATURE 3: Signal-Based State Management
  // ============================================================================
  describe('Signal-Based State Management', () => {
    it('should initialize password visibility signals as true (hidden)', () => {
      expect(component.hidePassword()).toBe(true);
      expect(component.hideConfirmPassword()).toBe(true);
    });

    it('should accept isSubmitting input signal', () => {
      expect(component.isSubmitting()).toBe(false);
      
      fixture.componentRef.setInput('isSubmitting', true);
      fixture.detectChanges();
      
      expect(component.isSubmitting()).toBe(true);
    });

    it('should accept serverError input signal', () => {
      expect(component.serverError()).toBeUndefined();
      
      fixture.componentRef.setInput('serverError', 'Network error');
      fixture.detectChanges();
      
      expect(component.serverError()).toBe('Network error');
    });

    it('should accept emailTaken input signal', () => {
      expect(component.emailTaken()).toBe(false);
      
      fixture.componentRef.setInput('emailTaken', true);
      fixture.detectChanges();
      
      expect(component.emailTaken()).toBe(true);
    });

    it('should emit submitted output when form is valid', () => {
      // Spy on the output
      const submittedSpy = jest.fn();
      component.submitted.subscribe(submittedSpy);
      
      // Fill form with valid data
      component.emailControl.setValue('test@example.com');
      component.passwordControl.setValue('validPassword123');
      component.confirmPasswordControl.setValue('validPassword123');
      component.form.updateValueAndValidity();
      
      // Submit form
      component.onSubmit();
      
      // Verify emission
      expect(submittedSpy).toHaveBeenCalledWith({
        email: 'test@example.com',
        password: 'validPassword123',
        confirmPassword: 'validPassword123'
      });
    });
  });

  // ============================================================================
  // FEATURE 4: Password Visibility Toggle
  // Includes edge cases: rapid visibility toggles
  // ============================================================================
  describe('Password Visibility Toggle', () => {
    it('should toggle password visibility', () => {
      expect(component.hidePassword()).toBe(true);
      
      component.togglePasswordVisibility();
      expect(component.hidePassword()).toBe(false);
      
      component.togglePasswordVisibility();
      expect(component.hidePassword()).toBe(true);
    });

    it('should toggle confirm password visibility', () => {
      expect(component.hideConfirmPassword()).toBe(true);
      
      component.toggleConfirmPasswordVisibility();
      expect(component.hideConfirmPassword()).toBe(false);
      
      component.toggleConfirmPasswordVisibility();
      expect(component.hideConfirmPassword()).toBe(true);
    });

    it('should toggle multiple times correctly', () => {
      // Initial state is true (hidden)
      // After 1st toggle: false, 2nd: true, 3rd: false, 4th: true, 5th: false
      for (let i = 0; i < 5; i++) {
        component.togglePasswordVisibility();
        expect(component.hidePassword()).toBe(i % 2 === 1);
      }
    });

    it('should maintain independent state for password and confirm password visibility', () => {
      component.togglePasswordVisibility();
      expect(component.hidePassword()).toBe(false);
      expect(component.hideConfirmPassword()).toBe(true);
      
      component.toggleConfirmPasswordVisibility();
      expect(component.hidePassword()).toBe(false);
      expect(component.hideConfirmPassword()).toBe(false);
    });

    it('should handle rapid visibility toggles', () => {
      for (let i = 0; i < 100; i++) {
        component.togglePasswordVisibility();
        component.toggleConfirmPasswordVisibility();
      }
      
      // Should not crash and should be in correct state (even number of toggles)
      expect(component.hidePassword()).toBe(true);
      expect(component.hideConfirmPassword()).toBe(true);
    });
  });

  // ============================================================================
  // FEATURE 5: Dynamic Server-Side Validation Integration
  // ============================================================================
  describe('Dynamic Server-Side Validation', () => {
    it('should set emailTaken error when input signal is true', () => {
      // Set valid email first
      component.emailControl.setValue('test@example.com');
      
      // Trigger emailTaken
      fixture.componentRef.setInput('emailTaken', true);
      fixture.detectChanges();
      
      expect(component.emailControl.hasError('emailTaken')).toBe(true);
    });

    it('should remove emailTaken error when input signal is false', () => {
      // Set email taken error first
      component.emailControl.setValue('test@example.com');
      fixture.componentRef.setInput('emailTaken', true);
      fixture.detectChanges();
      expect(component.emailControl.hasError('emailTaken')).toBe(true);
      
      // Remove error
      fixture.componentRef.setInput('emailTaken', false);
      fixture.detectChanges();
      
      expect(component.emailControl.hasError('emailTaken')).toBe(false);
    });

    it('should preserve other email errors when adding emailTaken error', () => {
      // Set invalid email format
      component.emailControl.setValue('invalid-email');
      component.emailControl.markAsTouched();
      expect(component.emailControl.hasError('email')).toBe(true);
      
      // Add emailTaken error
      fixture.componentRef.setInput('emailTaken', true);
      fixture.detectChanges();
      
      // Both errors should exist
      expect(component.emailControl.hasError('email')).toBe(true);
      expect(component.emailControl.hasError('emailTaken')).toBe(true);
    });

    it('should preserve other email errors when removing emailTaken error', () => {
      // Set invalid email and emailTaken error
      component.emailControl.setValue('invalid-email');
      component.emailControl.markAsTouched();
      fixture.componentRef.setInput('emailTaken', true);
      fixture.detectChanges();
      
      expect(component.emailControl.hasError('email')).toBe(true);
      expect(component.emailControl.hasError('emailTaken')).toBe(true);
      
      // Remove emailTaken error
      fixture.componentRef.setInput('emailTaken', false);
      fixture.detectChanges();
      
      // Email error should remain
      expect(component.emailControl.hasError('email')).toBe(true);
      expect(component.emailControl.hasError('emailTaken')).toBe(false);
    });
  });

  // ============================================================================
  // FEATURE 6: Real-Time Password Mismatch Validation
  // Includes edge cases: updating password while confirmPassword is already filled
  // ============================================================================
  describe('Real-Time Password Mismatch Validation', () => {
    it('should set passwordsMismatch error on confirmPassword control when passwords do not match', () => {
      component.passwordControl.setValue('password123');
      component.confirmPasswordControl.setValue('different');
      
      expect(component.confirmPasswordControl.hasError('passwordsMismatch')).toBe(true);
    });

    it('should remove passwordsMismatch error when passwords match', () => {
      // Set non-matching passwords
      component.passwordControl.setValue('password123');
      component.confirmPasswordControl.setValue('different');
      expect(component.confirmPasswordControl.hasError('passwordsMismatch')).toBe(true);
      
      // Make them match
      component.confirmPasswordControl.setValue('password123');
      
      expect(component.confirmPasswordControl.hasError('passwordsMismatch')).toBe(false);
    });

    it('should not set passwordsMismatch error when confirmPassword is empty', () => {
      component.passwordControl.setValue('password123');
      component.confirmPasswordControl.setValue('');
      
      expect(component.confirmPasswordControl.hasError('passwordsMismatch')).toBe(false);
    });

    it('should update error in real-time as user types in password field', () => {
      // Set initial matching passwords
      component.passwordControl.setValue('password123');
      component.confirmPasswordControl.setValue('password123');
      expect(component.confirmPasswordControl.hasError('passwordsMismatch')).toBe(false);
      
      // Type in password field
      component.passwordControl.setValue('password1234');
      
      // Error should appear immediately
      expect(component.confirmPasswordControl.hasError('passwordsMismatch')).toBe(true);
    });

    it('should update error in real-time as user types in confirmPassword field', () => {
      // Set non-matching passwords
      component.passwordControl.setValue('password123');
      component.confirmPasswordControl.setValue('pass');
      expect(component.confirmPasswordControl.hasError('passwordsMismatch')).toBe(true);
      
      // Continue typing to match
      component.confirmPasswordControl.setValue('password123');
      
      // Error should disappear immediately
      expect(component.confirmPasswordControl.hasError('passwordsMismatch')).toBe(false);
    });

    it('should handle updating password while confirmPassword is already filled', () => {
      // Fill both fields
      component.passwordControl.setValue('password123');
      component.confirmPasswordControl.setValue('password123');
      expect(component.form.hasError('passwordsMismatch')).toBe(false);
      
      // User goes back and changes password
      component.passwordControl.setValue('newPassword456');
      
      // Mismatch error should appear
      expect(component.form.hasError('passwordsMismatch')).toBe(true);
      expect(component.confirmPasswordControl.hasError('passwordsMismatch')).toBe(true);
    });
  });

  // ============================================================================
  // FEATURE 7 & 8: Comprehensive Error Messages & i18n Support
  // ============================================================================
  describe('Error Message Helpers', () => {
    describe('getEmailErrorMessage', () => {
      it('should return required error message', () => {
        component.emailControl.setValue('');
        component.emailControl.markAsTouched();
        
        const message = component.getEmailErrorMessage();
        expect(message).toContain('required');
      });

      it('should return invalid email error message', () => {
        component.emailControl.setValue('invalid-email');
        component.emailControl.markAsTouched();
        
        const message = component.getEmailErrorMessage();
        expect(message).toContain('valid email');
      });

      it('should return email taken error message', () => {
        component.emailControl.setValue('test@example.com');
        fixture.componentRef.setInput('emailTaken', true);
        fixture.detectChanges();
        
        const message = component.getEmailErrorMessage();
        expect(message).toContain('already registered');
      });

      it('should return empty string when no error', () => {
        component.emailControl.setValue('test@example.com');
        
        const message = component.getEmailErrorMessage();
        expect(message).toBe('');
      });
    });

    describe('getPasswordErrorMessage', () => {
      it('should return required error message', () => {
        component.passwordControl.setValue('');
        component.passwordControl.markAsTouched();
        
        const message = component.getPasswordErrorMessage();
        expect(message).toContain('required');
      });

      it('should return minlength error message with correct length', () => {
        component.passwordControl.setValue('short');
        component.passwordControl.markAsTouched();
        
        const message = component.getPasswordErrorMessage();
        expect(message).toContain('at least');
        expect(message).toContain('12');
      });

      it('should return empty string when no error', () => {
        component.passwordControl.setValue('validPassword123');
        
        const message = component.getPasswordErrorMessage();
        expect(message).toBe('');
      });
    });

    describe('getConfirmPasswordErrorMessage', () => {
      it('should return required error message', () => {
        component.confirmPasswordControl.setValue('');
        component.confirmPasswordControl.markAsTouched();
        
        const message = component.getConfirmPasswordErrorMessage();
        expect(message).toContain('confirm');
      });

      it('should return mismatch error message', () => {
        component.passwordControl.setValue('password123');
        component.confirmPasswordControl.setValue('different');
        component.confirmPasswordControl.markAsTouched();
        
        const message = component.getConfirmPasswordErrorMessage();
        expect(message).toContain('do not match');
      });

      it('should return empty string when no error', () => {
        component.passwordControl.setValue('password123');
        component.confirmPasswordControl.setValue('password123');
        
        const message = component.getConfirmPasswordErrorMessage();
        expect(message).toBe('');
      });
    });

    describe('getPasswordVisibilityLabel', () => {
      it('should return show label when password is hidden', () => {
        component.hidePassword.set(true);
        
        const label = component.getPasswordVisibilityLabel();
        expect(label).toContain('Show');
      });

      it('should return hide label when password is visible', () => {
        component.hidePassword.set(false);
        
        const label = component.getPasswordVisibilityLabel();
        expect(label).toContain('Hide');
      });
    });

    describe('getConfirmPasswordVisibilityLabel', () => {
      it('should return show label when password is hidden', () => {
        component.hideConfirmPassword.set(true);
        
        const label = component.getConfirmPasswordVisibilityLabel();
        expect(label).toContain('Show');
      });

      it('should return hide label when password is visible', () => {
        component.hideConfirmPassword.set(false);
        
        const label = component.getConfirmPasswordVisibilityLabel();
        expect(label).toContain('Hide');
      });
    });
  });

  // ============================================================================
  // FEATURE 9: Smart Error Management
  // ============================================================================
  describe('Smart Error Management', () => {
    it('should maintain multiple errors on the same control', () => {
      // Set invalid email format
      component.emailControl.setValue('invalid');
      expect(component.emailControl.hasError('email')).toBe(true);
      
      // Add emailTaken error
      fixture.componentRef.setInput('emailTaken', true);
      fixture.detectChanges();
      
      // Both should coexist
      expect(component.emailControl.hasError('email')).toBe(true);
      expect(component.emailControl.hasError('emailTaken')).toBe(true);
    });

    it('should preserve required error when removing other errors', () => {
      component.confirmPasswordControl.setValue('');
      component.confirmPasswordControl.markAsTouched();
      
      // Has required error
      expect(component.confirmPasswordControl.hasError('required')).toBe(true);
      
      // Try to remove non-existent passwordsMismatch error
      component.passwordControl.setValue('anything');
      
      // Required error should remain
      expect(component.confirmPasswordControl.hasError('required')).toBe(true);
    });

    it('should handle error removal gracefully when control has no errors', () => {
      // Set valid email
      component.emailControl.setValue('test@example.com');
      expect(component.emailControl.errors).toBeNull();
      
      // Try to remove emailTaken error (which doesn't exist)
      fixture.componentRef.setInput('emailTaken', false);
      fixture.detectChanges();
      
      // Should still be null
      expect(component.emailControl.errors).toBeNull();
    });
  });

  // ============================================================================
  // FEATURE 10: Form Submission Logic
  // ============================================================================
  describe('Form Submission', () => {
    it('should not emit when form is invalid', () => {
      const submittedSpy = jest.fn();
      component.submitted.subscribe(submittedSpy);
      
      // Leave form empty (invalid)
      component.onSubmit();
      
      expect(submittedSpy).not.toHaveBeenCalled();
    });

    it('should not emit when isSubmitting is true', () => {
      const submittedSpy = jest.fn();
      component.submitted.subscribe(submittedSpy);
      
      // Fill form with valid data
      component.emailControl.setValue('test@example.com');
      component.passwordControl.setValue('validPassword123');
      component.confirmPasswordControl.setValue('validPassword123');
      
      // Set isSubmitting to true
      fixture.componentRef.setInput('isSubmitting', true);
      fixture.detectChanges();
      
      component.onSubmit();
      
      expect(submittedSpy).not.toHaveBeenCalled();
    });

    it('should mark all controls as touched on submit', () => {
      component.onSubmit();
      
      expect(component.emailControl.touched).toBe(true);
      expect(component.passwordControl.touched).toBe(true);
      expect(component.confirmPasswordControl.touched).toBe(true);
    });

    it('should emit correct form value when valid', () => {
      const submittedSpy = jest.fn();
      component.submitted.subscribe(submittedSpy);
      
      const formData: RegisterFormModel = {
        email: 'user@example.com',
        password: 'securePassword123',
        confirmPassword: 'securePassword123'
      };
      
      component.emailControl.setValue(formData.email);
      component.passwordControl.setValue(formData.password);
      component.confirmPasswordControl.setValue(formData.confirmPassword);
      
      component.onSubmit();
      
      expect(submittedSpy).toHaveBeenCalledWith(formData);
      expect(submittedSpy).toHaveBeenCalledTimes(1);
    });

    it('should not emit when passwords do not match', () => {
      const submittedSpy = jest.fn();
      component.submitted.subscribe(submittedSpy);
      
      component.emailControl.setValue('test@example.com');
      component.passwordControl.setValue('validPassword123');
      component.confirmPasswordControl.setValue('differentPassword456');
      
      component.onSubmit();
      
      expect(submittedSpy).not.toHaveBeenCalled();
    });

    it('should handle edge case: password just meets minimum length', () => {
      const submittedSpy = jest.fn();
      component.submitted.subscribe(submittedSpy);
      
      const minLengthPassword = '123456789012'; // Exactly 12 characters
      
      component.emailControl.setValue('test@example.com');
      component.passwordControl.setValue(minLengthPassword);
      component.confirmPasswordControl.setValue(minLengthPassword);
      
      expect(component.form.valid).toBe(true);
      
      component.onSubmit();
      
      expect(submittedSpy).toHaveBeenCalledWith({
        email: 'test@example.com',
        password: minLengthPassword,
        confirmPassword: minLengthPassword
      });
    });
  });

  // ============================================================================
  // Integration Tests: Multiple Features Working Together
  // ============================================================================
  describe('Integration Tests', () => {
    it('should handle complete user registration flow', () => {
      const submittedSpy = jest.fn();
      component.submitted.subscribe(submittedSpy);
      
      // User types email
      component.emailControl.setValue('newuser@example.com');
      expect(component.emailControl.valid).toBe(true);
      
      // User types password
      component.passwordControl.setValue('securePassword123');
      expect(component.passwordControl.valid).toBe(true);
      
      // User types confirm password (initially wrong)
      component.confirmPasswordControl.setValue('wrongPassword');
      expect(component.form.hasError('passwordsMismatch')).toBe(true);
      expect(component.confirmPasswordControl.hasError('passwordsMismatch')).toBe(true);
      
      // User corrects confirm password
      component.confirmPasswordControl.setValue('securePassword123');
      expect(component.form.hasError('passwordsMismatch')).toBe(false);
      expect(component.form.valid).toBe(true);
      
      // User toggles password visibility
      component.togglePasswordVisibility();
      expect(component.hidePassword()).toBe(false);
      
      // User submits form
      component.onSubmit();
      
      expect(submittedSpy).toHaveBeenCalledWith({
        email: 'newuser@example.com',
        password: 'securePassword123',
        confirmPassword: 'securePassword123'
      });
    });

    it('should handle server-side validation rejection and retry', () => {
      const submittedSpy = jest.fn();
      component.submitted.subscribe(submittedSpy);
      
      // Fill form
      component.emailControl.setValue('taken@example.com');
      component.passwordControl.setValue('validPassword123');
      component.confirmPasswordControl.setValue('validPassword123');
      
      // First submit
      component.onSubmit();
      expect(submittedSpy).toHaveBeenCalledTimes(1);
      
      // Server responds with email taken
      fixture.componentRef.setInput('emailTaken', true);
      fixture.detectChanges();
      
      expect(component.emailControl.hasError('emailTaken')).toBe(true);
      expect(component.form.valid).toBe(false);
      
      // User tries to submit again (should not emit)
      component.onSubmit();
      expect(submittedSpy).toHaveBeenCalledTimes(1); // Still 1
      
      // User changes email
      component.emailControl.setValue('different@example.com');
      fixture.componentRef.setInput('emailTaken', false);
      fixture.detectChanges();
      
      expect(component.emailControl.hasError('emailTaken')).toBe(false);
      expect(component.form.valid).toBe(true);
      
      // User submits again
      component.onSubmit();
      expect(submittedSpy).toHaveBeenCalledTimes(2);
      expect(submittedSpy).toHaveBeenLastCalledWith({
        email: 'different@example.com',
        password: 'validPassword123',
        confirmPassword: 'validPassword123'
      });
    });

    it('should prevent submission while already submitting', () => {
      const submittedSpy = jest.fn();
      component.submitted.subscribe(submittedSpy);
      
      // Fill form
      component.emailControl.setValue('test@example.com');
      component.passwordControl.setValue('validPassword123');
      component.confirmPasswordControl.setValue('validPassword123');
      
      // First submit
      component.onSubmit();
      expect(submittedSpy).toHaveBeenCalledTimes(1);
      
      // Simulate submission in progress
      fixture.componentRef.setInput('isSubmitting', true);
      fixture.detectChanges();
      
      // Try to submit again
      component.onSubmit();
      
      // Should still be 1
      expect(submittedSpy).toHaveBeenCalledTimes(1);
    });

    it('should handle all validation errors simultaneously', () => {
      // Empty email
      component.emailControl.setValue('');
      component.emailControl.markAsTouched();
      expect(component.getEmailErrorMessage()).toContain('required');
      
      // Invalid email format
      component.emailControl.setValue('invalid');
      expect(component.getEmailErrorMessage()).toContain('valid email');
      
      // Email taken
      component.emailControl.setValue('test@example.com');
      fixture.componentRef.setInput('emailTaken', true);
      fixture.detectChanges();
      expect(component.getEmailErrorMessage()).toContain('already registered');
      
      // Short password
      component.passwordControl.setValue('short');
      component.passwordControl.markAsTouched();
      expect(component.getPasswordErrorMessage()).toContain('at least');
      
      // Password mismatch
      component.passwordControl.setValue('validPassword123');
      component.confirmPasswordControl.setValue('different');
      component.confirmPasswordControl.markAsTouched();
      expect(component.getConfirmPasswordErrorMessage()).toContain('do not match');
      
      // Form should be invalid
      expect(component.form.valid).toBe(false);
    });
  });

});

