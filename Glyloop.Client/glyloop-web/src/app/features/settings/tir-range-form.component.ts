import { Component, ChangeDetectionStrategy, input, output, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { AccountPreferencesVM, UpdatePreferencesRequestDto, ValidationErrors } from '../../core/models/settings.types';

/**
 * TIR Range Form component
 * Numeric inputs and optional dual-range slider with inline errors and helper text
 */
@Component({
  selector: 'app-tir-range-form',
  standalone: true,
  imports: [CommonModule, FormsModule, MatButtonModule, MatFormFieldModule, MatInputModule],
  templateUrl: './tir-range-form.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TirRangeFormComponent implements OnInit {
  readonly value = input.required<AccountPreferencesVM>();
  readonly save = output<UpdatePreferencesRequestDto>();
  readonly cancel = output<void>();

  readonly localLower = signal<number>(70);
  readonly localUpper = signal<number>(180);
  readonly errors = signal<ValidationErrors>({});

  readonly isDirty = computed(() => {
    const v = this.value();
    return this.localLower() !== v.initialLower || this.localUpper() !== v.initialUpper;
  });

  readonly isValid = computed(() => {
    const errs = this.errors();
    return !errs.lower && !errs.upper && !errs.cross;
  });

  ngOnInit(): void {
    const v = this.value();
    this.localLower.set(v.lower);
    this.localUpper.set(v.upper);
  }

  onValueChange(): void {
    this.validate();
  }

  onBlur(): void {
    this.validate();
  }

  validate(): void {
    const lower = this.localLower();
    const upper = this.localUpper();
    const newErrors: ValidationErrors = {};

    // Validate lower bound
    if (lower < 0 || lower > 1000) {
      newErrors.lower = $localize`:@@settings.tir.error.lowerOutOfRange:Lower bound must be between 0 and 1000 mg/dL`;
    }

    // Validate upper bound
    if (upper < 0 || upper > 1000) {
      newErrors.upper = $localize`:@@settings.tir.error.upperOutOfRange:Upper bound must be between 0 and 1000 mg/dL`;
    }

    // Cross-field validation
    if (lower >= upper) {
      newErrors.cross = $localize`:@@settings.tir.error.lowerGreaterThanUpper:Lower bound must be less than upper bound`;
    }

    this.errors.set(newErrors);
  }

  onSubmit(): void {
    this.validate();
    if (this.isValid() && this.isDirty()) {
      this.save.emit({
        tirLowerBound: this.localLower(),
        tirUpperBound: this.localUpper()
      });
    }
  }

  onCancel(): void {
    const v = this.value();
    this.localLower.set(v.initialLower);
    this.localUpper.set(v.initialUpper);
    this.errors.set({});
    this.cancel.emit();
  }
}
