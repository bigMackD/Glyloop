import { Component, ChangeDetectionStrategy, input, output, signal, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { debounceTime } from 'rxjs';
import { HistoryFilters, EventType } from '../../../core/models/dashboard.types';

/**
 * History filter bar with date range, event type selection, and pagination.
 */
@Component({
  selector: 'app-history-filter-bar',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatDatepickerModule,
    MatNativeDateModule
  ],
  templateUrl: './history-filter-bar.component.html',
  styleUrl: './history-filter-bar.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class HistoryFilterBarComponent {
  private readonly fb = new FormBuilder();

  // Input
  readonly filters = input.required<HistoryFilters>();

  // Outputs
  readonly filtersChange = output<HistoryFilters>();
  readonly reset = output<void>();

  // Available event types
  readonly eventTypes: EventType[] = ['Food', 'Insulin', 'Exercise', 'Note'];

  // Form
  readonly filterForm = this.fb.group({
    fromDate: [null as Date | null],
    toDate: [null as Date | null],
    eventType: [null as EventType | null]
  });

  // Localized strings
  readonly fromDateLabel = $localize`:@@dashboard.history.filter.fromDate:From Date`;
  readonly toDateLabel = $localize`:@@dashboard.history.filter.toDate:To Date`;
  readonly eventTypesLabel = $localize`:@@dashboard.history.filter.eventTypes:Event Types`;
  readonly applyLabel = $localize`:@@dashboard.history.filter.apply:Apply`;
  readonly resetLabel = $localize`:@@dashboard.history.filter.reset:Reset`;
  readonly allTypesLabel = $localize`:@@dashboard.history.filter.allTypes:All Types`;

  constructor() {
    // Initialize form with input filters
    effect(() => {
      const currentFilters = this.filters();
      this.updateFormFromFilters(currentFilters);
    });

    // Debounced form changes
    this.filterForm.valueChanges.pipe(debounceTime(500)).subscribe(() => {
      this.applyFilters();
    });
  }

  /**
   * Updates form values from filters input
   */
  private updateFormFromFilters(filters: HistoryFilters): void {
    const fromDate = filters.fromDateUtc ? new Date(filters.fromDateUtc) : null;
    const toDate = filters.toDateUtc ? new Date(filters.toDateUtc) : null;

    this.filterForm.patchValue(
      {
        fromDate,
        toDate,
        eventType: filters.types && filters.types.length > 0 ? filters.types[0] : null
      },
      { emitEvent: false }
    );
  }

  /**
   * Applies current form values as filters
   */
  applyFilters(): void {
    const formValue = this.filterForm.value;
    const currentFilters = this.filters();

    // Validate date range
    if (formValue.fromDate && formValue.toDate) {
      if (formValue.fromDate > formValue.toDate) {
        // Invalid range - don't apply
        return;
      }
    }

    const newFilters: HistoryFilters = {
      ...currentFilters,
      fromDateUtc: formValue.fromDate ? formValue.fromDate.toISOString() : undefined,
      toDateUtc: formValue.toDate ? formValue.toDate.toISOString() : undefined,
      types: formValue.eventType ? [formValue.eventType] : undefined,
      page: 1 // Reset to first page when filters change
    };

    this.filtersChange.emit(newFilters);
  }

  /**
   * Resets filters to defaults
   */
  onReset(): void {
    this.filterForm.reset({
      fromDate: null,
      toDate: null,
      eventType: null
    });

    this.reset.emit();
  }

  /**
   * Gets validation error message for date range
   */
  getDateRangeError(): string | null {
    const fromDate = this.filterForm.value.fromDate;
    const toDate = this.filterForm.value.toDate;

    if (fromDate && toDate && fromDate > toDate) {
      return $localize`:@@dashboard.history.filter.error.invalidDateRange:From date must be before To date`;
    }

    return null;
  }
}
