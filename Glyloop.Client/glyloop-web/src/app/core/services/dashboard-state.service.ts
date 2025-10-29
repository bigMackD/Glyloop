import { Injectable, signal, computed } from '@angular/core';
import { ChartRange, PollState } from '../models/dashboard.types';

/**
 * Dashboard state management service using Angular signals.
 * Manages the active chart range, selected event, and polling state.
 */
@Injectable({ providedIn: 'root' })
export class DashboardStateService {
  // State signals
  private readonly _activeRange = signal<ChartRange>('3h'); // Default 3 hours
  private readonly _selectedEventId = signal<string | undefined>(undefined);
  private readonly _pollState = signal<PollState>({ status: 'idle' });

  // Public read-only signals
  readonly activeRange = this._activeRange.asReadonly();
  readonly selectedEventId = this._selectedEventId.asReadonly();
  readonly pollState = this._pollState.asReadonly();

  // Computed signals
  readonly rangeSeconds = computed(() => {
    const range = this._activeRange();
    const hours = parseInt(range.replace('h', ''), 10);
    return hours * 3600;
  });

  /**
   * Sets the active chart range
   */
  setRange(range: ChartRange): void {
    this._activeRange.set(range);
  }

  /**
   * Selects an event by ID (for highlighting and details)
   */
  selectEvent(id: string | undefined): void {
    this._selectedEventId.set(id);
  }

  /**
   * Updates the polling state
   */
  setPollState(state: PollState): void {
    this._pollState.set(state);
  }

  /**
   * Resets dashboard state to defaults
   */
  reset(): void {
    this._activeRange.set('3h');
    this._selectedEventId.set(undefined);
    this._pollState.set({ status: 'idle' });
  }
}
