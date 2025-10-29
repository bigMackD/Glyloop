import { Component, ChangeDetectionStrategy, input, output, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDialogModule } from '@angular/material/dialog';
import { MatTabsModule } from '@angular/material/tabs';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { EventsService } from '../../../core/services/events.service';
import { EventResponseDto } from '../../../core/models/dashboard.types';
import { catchError, finalize, of } from 'rxjs';

/**
 * Add Event modal component with tabbed forms for Food, Insulin, Exercise, and Note events.
 */
@Component({
  selector: 'app-add-event-modal',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatTabsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './add-event-modal.component.html',
  styleUrl: './add-event-modal.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AddEventModalComponent {
  private readonly fb = new FormBuilder();
  private readonly eventsService = inject(EventsService);

  // Input
  readonly open = input.required<boolean>();

  // Outputs
  readonly close = output<void>();
  readonly created = output<EventResponseDto>();

  // State
  readonly activeTabIndex = signal<number>(0);
  readonly isSubmitting = signal<boolean>(false);
  readonly error = signal<string | undefined>(undefined);

  // Forms
  readonly foodForm = this.fb.group({
    carbs_g: [
      null as number | null,
      [Validators.required, Validators.min(0), Validators.max(300)]
    ],
    meal_tag: [''],
    absorption_hint: [''],
    note: ['', Validators.maxLength(500)]
  });

  readonly insulinForm = this.fb.group({
    insulin_units: [
      null as number | null,
      [Validators.required, Validators.min(0), Validators.max(100)]
    ],
    preparation: [''],
    delivery: [''],
    timing: [''],
    note: ['', Validators.maxLength(500)]
  });

  readonly exerciseForm = this.fb.group({
    type: ['', Validators.required],
    duration_min: [
      null as number | null,
      [Validators.required, Validators.min(1), Validators.max(300)]
    ],
    intensity: [''],
    start_time_utc: ['']
  });

  readonly noteForm = this.fb.group({
    note: ['', [Validators.required, Validators.maxLength(500)]]
  });

  // Options for selects
  readonly mealTags = ['Breakfast', 'Lunch', 'Dinner', 'Snack'];
  readonly absorptionHints = ['Fast', 'Medium', 'Slow'];
  readonly insulinPreparations = ['Rapid', 'Short', 'Intermediate', 'Long'];
  readonly insulinDeliveries = ['Injection', 'Pump'];
  readonly insulinTimings = ['Before meal', 'With meal', 'After meal'];
  readonly exerciseTypes = ['Walking', 'Running', 'Cycling', 'Swimming', 'Strength', 'Sports', 'Other'];
  readonly intensities = ['Low', 'Moderate', 'High'];

  // Localized strings
  readonly title = $localize`:@@dashboard.addEvent.title:Add Event`;
  readonly foodTab = $localize`:@@dashboard.addEvent.tabs.food:Food`;
  readonly insulinTab = $localize`:@@dashboard.addEvent.tabs.insulin:Insulin`;
  readonly exerciseTab = $localize`:@@dashboard.addEvent.tabs.exercise:Exercise`;
  readonly noteTab = $localize`:@@dashboard.addEvent.tabs.note:Note`;
  readonly submitLabel = $localize`:@@dashboard.addEvent.submit:Create Event`;
  readonly cancelLabel = $localize`:@@dashboard.addEvent.cancel:Cancel`;

  // Food labels
  readonly carbsLabel = $localize`:@@dashboard.addEvent.food.carbs:Carbohydrates (g)`;
  readonly mealTagLabel = $localize`:@@dashboard.addEvent.food.mealTag:Meal Tag`;
  readonly absorptionLabel = $localize`:@@dashboard.addEvent.food.absorption:Absorption`;
  readonly noteLabel = $localize`:@@dashboard.addEvent.note:Note`;

  // Insulin labels
  readonly insulinUnitsLabel = $localize`:@@dashboard.addEvent.insulin.units:Units`;
  readonly preparationLabel = $localize`:@@dashboard.addEvent.insulin.preparation:Preparation`;
  readonly deliveryLabel = $localize`:@@dashboard.addEvent.insulin.delivery:Delivery`;
  readonly timingLabel = $localize`:@@dashboard.addEvent.insulin.timing:Timing`;

  // Exercise labels
  readonly exerciseTypeLabel = $localize`:@@dashboard.addEvent.exercise.type:Exercise Type`;
  readonly durationLabel = $localize`:@@dashboard.addEvent.exercise.duration:Duration (minutes)`;
  readonly intensityLabel = $localize`:@@dashboard.addEvent.exercise.intensity:Intensity`;

  /**
   * Handles tab change and resets forms
   */
  onTabChange(index: number): void {
    this.activeTabIndex.set(index);
    this.error.set(undefined);
    this.resetAllForms();
  }

  /**
   * Resets all forms
   */
  private resetAllForms(): void {
    this.foodForm.reset();
    this.insulinForm.reset();
    this.exerciseForm.reset();
    this.noteForm.reset();
  }

  /**
   * Handles form submission based on active tab
   */
  onSubmit(): void {
    const tabIndex = this.activeTabIndex();

    switch (tabIndex) {
      case 0:
        this.submitFoodEvent();
        break;
      case 1:
        this.submitInsulinEvent();
        break;
      case 2:
        this.submitExerciseEvent();
        break;
      case 3:
        this.submitNoteEvent();
        break;
    }
  }

  /**
   * Submits Food event
   */
  private submitFoodEvent(): void {
    if (this.foodForm.invalid) {
      this.foodForm.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.error.set(undefined);

    const value = this.foodForm.value;
    const payload = {
      carbs_g: value.carbs_g!,
      meal_tag: value.meal_tag || undefined,
      absorption_hint: value.absorption_hint || undefined,
      note: value.note || undefined
    };

    this.eventsService
      .createFood(payload)
      .pipe(
        finalize(() => this.isSubmitting.set(false)),
        catchError((err) => {
          this.handleError(err);
          return of(null);
        })
      )
      .subscribe((result) => {
        if (result) {
          this.onSuccess(result);
        }
      });
  }

  /**
   * Submits Insulin event
   */
  private submitInsulinEvent(): void {
    if (this.insulinForm.invalid) {
      this.insulinForm.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.error.set(undefined);

    const value = this.insulinForm.value;
    const payload = {
      insulin_units: value.insulin_units!,
      preparation: value.preparation || undefined,
      delivery: value.delivery || undefined,
      timing: value.timing || undefined,
      note: value.note || undefined
    };

    this.eventsService
      .createInsulin(payload)
      .pipe(
        finalize(() => this.isSubmitting.set(false)),
        catchError((err) => {
          this.handleError(err);
          return of(null);
        })
      )
      .subscribe((result) => {
        if (result) {
          this.onSuccess(result);
        }
      });
  }

  /**
   * Submits Exercise event
   */
  private submitExerciseEvent(): void {
    if (this.exerciseForm.invalid) {
      this.exerciseForm.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.error.set(undefined);

    const value = this.exerciseForm.value;
    const payload = {
      type: value.type!,
      duration_min: value.duration_min!,
      intensity: value.intensity || undefined,
      start_time_utc: value.start_time_utc || undefined
    };

    this.eventsService
      .createExercise(payload)
      .pipe(
        finalize(() => this.isSubmitting.set(false)),
        catchError((err) => {
          this.handleError(err);
          return of(null);
        })
      )
      .subscribe((result) => {
        if (result) {
          this.onSuccess(result);
        }
      });
  }

  /**
   * Submits Note event
   */
  private submitNoteEvent(): void {
    if (this.noteForm.invalid) {
      this.noteForm.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.error.set(undefined);

    const value = this.noteForm.value;
    const payload = {
      note: value.note!
    };

    this.eventsService
      .createNote(payload)
      .pipe(
        finalize(() => this.isSubmitting.set(false)),
        catchError((err) => {
          this.handleError(err);
          return of(null);
        })
      )
      .subscribe((result) => {
        if (result) {
          this.onSuccess(result);
        }
      });
  }

  /**
   * Handles successful event creation
   */
  private onSuccess(event: EventResponseDto): void {
    this.resetAllForms();
    this.created.emit(event);
    this.onClose();
  }

  /**
   * Handles errors from API
   */
  private handleError(err: any): void {
    if (err.status === 400) {
      this.error.set(
        err.error?.detail ||
          $localize`:@@dashboard.addEvent.error.validation:Invalid input. Please check your entries.`
      );
    } else {
      this.error.set(
        $localize`:@@dashboard.addEvent.error.generic:Failed to create event. Please try again.`
      );
    }
  }

  /**
   * Closes the modal
   */
  onClose(): void {
    this.resetAllForms();
    this.error.set(undefined);
    this.close.emit();
  }

  /**
   * Gets current form based on active tab
   */
  getCurrentForm() {
    const tabIndex = this.activeTabIndex();
    switch (tabIndex) {
      case 0:
        return this.foodForm;
      case 1:
        return this.insulinForm;
      case 2:
        return this.exerciseForm;
      case 3:
        return this.noteForm;
      default:
        return this.foodForm;
    }
  }
}
