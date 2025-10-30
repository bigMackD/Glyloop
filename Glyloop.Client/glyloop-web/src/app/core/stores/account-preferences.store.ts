import { Injectable, signal, computed, inject } from '@angular/core';
import { SettingsApiService } from '../services/settings-api.service';
import { AccountPreferencesVM, TirPreferencesDto, UpdatePreferencesRequestDto, ValidationErrors } from '../models/settings.types';
import { catchError, tap, of } from 'rxjs';

/**
 * Store for managing account preferences state
 * Uses Angular signals for reactive state management
 */
@Injectable({ providedIn: 'root' })
export class AccountPreferencesStore {
  private readonly settingsApi = inject(SettingsApiService);

  // State signals
  private readonly _preferences = signal<AccountPreferencesVM>({
    lower: 70,
    upper: 180,
    initialLower: 70,
    initialUpper: 180,
    isDirty: false,
    isValid: true,
    errors: {},
    saving: false
  });

  private readonly _loading = signal<boolean>(false);
  private readonly _error = signal<string | null>(null);

  // Public readonly signals
  readonly preferences = this._preferences.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly error = this._error.asReadonly();

  // Computed signals
  readonly isDirty = computed(() => this._preferences().isDirty);
  readonly isValid = computed(() => this._preferences().isValid);

  /**
   * Loads preferences from the API
   */
  load(): void {
    this._loading.set(true);
    this._error.set(null);

    this.settingsApi.getPreferences().pipe(
      tap((data: TirPreferencesDto) => {
        this._preferences.set({
          lower: data.tirLowerBound,
          upper: data.tirUpperBound,
          initialLower: data.tirLowerBound,
          initialUpper: data.tirUpperBound,
          isDirty: false,
          isValid: true,
          errors: {},
          saving: false
        });
        this._loading.set(false);
      }),
      catchError((err) => {
        console.error('Failed to load preferences:', err);
        this._error.set('Could not load your preferences. Please try again later.');
        this._loading.set(false);
        return of(null);
      })
    ).subscribe();
  }

  /**
   * Updates specific fields in the preferences
   */
  edit(partial: Partial<AccountPreferencesVM>): void {
    const current = this._preferences();
    this._preferences.set({ ...current, ...partial });
  }

  /**
   * Validates the current preferences
   */
  validate(): void {
    const current = this._preferences();
    const errors: ValidationErrors = {};
    let isValid = true;

    if (current.lower < 0 || current.lower > 1000) {
      errors.lower = 'Lower bound must be between 0 and 1000 mg/dL';
      isValid = false;
    }

    if (current.upper < 0 || current.upper > 1000) {
      errors.upper = 'Upper bound must be between 0 and 1000 mg/dL';
      isValid = false;
    }

    if (current.lower >= current.upper) {
      errors.cross = 'Lower bound must be less than upper bound';
      isValid = false;
    }

    this._preferences.set({ ...current, errors, isValid });
  }

  /**
   * Saves the current preferences to the API
   */
  save(data: UpdatePreferencesRequestDto): Promise<{ success: boolean; error?: string }> {
    return new Promise((resolve) => {
      const current = this._preferences();
      this._preferences.set({ ...current, saving: true });

      this.settingsApi.updatePreferences(data).pipe(
        tap(() => {
          this._preferences.set({
            lower: data.tirLowerBound,
            upper: data.tirUpperBound,
            initialLower: data.tirLowerBound,
            initialUpper: data.tirUpperBound,
            isDirty: false,
            isValid: true,
            errors: {},
            saving: false
          });
          resolve({ success: true });
        }),
        catchError((err) => {
          console.error('Failed to save preferences:', err);
          this._preferences.set({ ...current, saving: false });

          // Extract error message from API response
          const errorMessage = err.error?.message || 'Could not save preferences. Please try again.';
          resolve({ success: false, error: errorMessage });
          return of(null);
        })
      ).subscribe();
    });
  }

  /**
   * Resets preferences to initial values
   */
  reset(): void {
    const current = this._preferences();
    this._preferences.set({
      ...current,
      lower: current.initialLower,
      upper: current.initialUpper,
      isDirty: false,
      errors: {}
    });
  }
}
