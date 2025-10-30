import { Injectable, signal, computed, inject } from '@angular/core';
import { DexcomApiService } from '../services/dexcom-api.service';
import { DexcomLinkVM, DexcomStatusDto } from '../models/settings.types';
import { catchError, tap, of } from 'rxjs';

/**
 * Store for managing Dexcom link state
 * Uses Angular signals for reactive state management
 */
@Injectable({ providedIn: 'root' })
export class DexcomStore {
  private readonly dexcomApi = inject(DexcomApiService);

  // State signal
  private readonly _state = signal<DexcomLinkVM>({
    status: null,
    loading: false,
    linking: false,
    unlinking: false,
    error: undefined
  });

  // Public readonly signal
  readonly state = this._state.asReadonly();

  // Computed signals
  readonly isLinked = computed(() => this._state().status?.isLinked ?? false);
  readonly loading = computed(() => this._state().loading);
  readonly linking = computed(() => this._state().linking);
  readonly unlinking = computed(() => this._state().unlinking);
  readonly error = computed(() => this._state().error);

  /**
   * Loads the current Dexcom status from the API
   */
  loadStatus(): void {
    const current = this._state();
    this._state.set({ ...current, loading: true, error: undefined });

    this.dexcomApi.getDexcomStatus().pipe(
      tap((status: DexcomStatusDto) => {
        const current = this._state();
        this._state.set({ ...current, status, loading: false });
      }),
      catchError((err) => {
        console.error('Failed to load Dexcom status:', err);
        const current = this._state();
        this._state.set({
          ...current,
          loading: false,
          error: 'Could not load Dexcom status. Please try again.'
        });
        return of(null);
      })
    ).subscribe();
  }

  /**
   * Initiates the Dexcom OAuth flow
   */
  startLinkFlow(): void {
    this.dexcomApi.startOAuthFlow();
  }

  /**
   * Completes the Dexcom link using an authorization code
   */
  link(code: string): Promise<{ success: boolean; error?: string }> {
    return new Promise((resolve) => {
      const current = this._state();
      this._state.set({ ...current, linking: true, error: undefined });

      this.dexcomApi.linkDexcom(code).pipe(
        tap((response) => {
          // Update status with the new link information
          const newStatus: DexcomStatusDto = {
            isLinked: true,
            linkedAt: response.linkedAt,
            tokenExpiresAt: response.tokenExpiresAt,
            lastSyncAt: null
          };

          const current = this._state();
          this._state.set({ ...current, status: newStatus, linking: false });
          resolve({ success: true });
        }),
        catchError((err) => {
          console.error('Failed to link Dexcom:', err);
          const current = this._state();

          // Extract error message from API response
          const errorMessage = err.error?.message || 'Could not link Dexcom account. Please try again.';
          this._state.set({ ...current, linking: false, error: errorMessage });
          resolve({ success: false, error: errorMessage });
          return of(null);
        })
      ).subscribe();
    });
  }

  /**
   * Unlinks the Dexcom account
   */
  unlink(): Promise<{ success: boolean; error?: string }> {
    return new Promise((resolve) => {
      const current = this._state();
      this._state.set({ ...current, unlinking: true, error: undefined });

      this.dexcomApi.unlinkDexcom().pipe(
        tap(() => {
          // Update status to reflect unlinked state
          const newStatus: DexcomStatusDto = {
            isLinked: false,
            linkedAt: null,
            tokenExpiresAt: null,
            lastSyncAt: null
          };

          const current = this._state();
          this._state.set({ ...current, status: newStatus, unlinking: false });
          resolve({ success: true });
        }),
        catchError((err) => {
          console.error('Failed to unlink Dexcom:', err);
          const current = this._state();

          // If 404, treat as already unlinked
          if (err.status === 404) {
            const newStatus: DexcomStatusDto = {
              isLinked: false,
              linkedAt: null,
              tokenExpiresAt: null,
              lastSyncAt: null
            };
            this._state.set({ ...current, status: newStatus, unlinking: false });
            resolve({ success: true });
          } else {
            const errorMessage = err.error?.message || 'Could not unlink Dexcom account. Please try again.';
            this._state.set({ ...current, unlinking: false, error: errorMessage });
            resolve({ success: false, error: errorMessage });
          }
          return of(null);
        })
      ).subscribe();
    });
  }

  /**
   * Sets an error message
   */
  setError(error: string): void {
    const current = this._state();
    this._state.set({ ...current, error });
  }

  /**
   * Clears the error message
   */
  clearError(): void {
    const current = this._state();
    this._state.set({ ...current, error: undefined });
  }
}
