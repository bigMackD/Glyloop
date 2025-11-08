import { Component, ChangeDetectionStrategy, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ChartRange, PollState } from '../../../core/models/dashboard.types';

/**
 * Chart toolbar with range selection buttons, polling status chip, and timezone note.
 */
@Component({
  selector: 'app-chart-toolbar',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './chart-toolbar.component.html',
  styleUrl: './chart-toolbar.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ChartToolbarComponent {
  // Inputs
  readonly activeRange = input.required<ChartRange>();
  readonly pollState = input.required<PollState>();

  // Outputs
  readonly rangeChange = output<ChartRange>();
  readonly manualRefresh = output<void>();

  // Available ranges
  readonly ranges: ChartRange[] = [1, 3, 5, 8, 12, 24];

  // Localized strings
  readonly timezoneNote = $localize`:@@dashboard.toolbar.timezoneNote:All times local; range computed in UTC`;

  /**
   * Gets the display status text based on poll state
   */
  getPollStatusText(): string {
    const state = this.pollState();

    switch (state.status) {
      case 'idle':
        return $localize`:@@dashboard.toolbar.pollStatus.idle:Idle`;
      case 'ok':
        return $localize`:@@dashboard.toolbar.pollStatus.ok:Up to date`;
      case 'paused':
        return $localize`:@@dashboard.toolbar.pollStatus.paused:Paused`;
      case 'backoff': {
        const minutes = Math.round(
          (state.nextRetryAt.getTime() - Date.now()) / 60000
        );
        return $localize`:@@dashboard.toolbar.pollStatus.backoff:Retry in ${minutes}m`;
      }
      case 'error':
        return $localize`:@@dashboard.toolbar.pollStatus.error:Error`;
      default:
        return '';
    }
  }

  /**
   * Gets the CSS class for the poll status chip
   */
  getPollStatusClass(): string {
    const state = this.pollState();
    return `poll-status-${state.status}`;
  }

  /**
   * Gets the icon for the poll status chip
   */
  getPollStatusIcon(): string {
    const state = this.pollState();

    switch (state.status) {
      case 'ok':
        return 'check_circle';
      case 'paused':
        return 'pause_circle';
      case 'backoff':
        return 'schedule';
      case 'error':
        return 'error';
      default:
        return 'info';
    }
  }

  /**
   * Determines if the status chip should be clickable (for manual refresh)
   */
  canManualRefresh(): boolean {
    const state = this.pollState();
    return state.status === 'error' || state.status === 'backoff';
  }

  /**
   * Handles range selection
   */
  onRangeChange(range: ChartRange): void {
    if (range !== this.activeRange()) {
      this.rangeChange.emit(range);
    }
  }

  /**
   * Handles manual refresh click
   */
  onManualRefresh(): void {
    if (this.canManualRefresh()) {
      this.manualRefresh.emit();
    }
  }

  /**
   * Gets the CSS class for range buttons
   */
  getRangeButtonClass(range: ChartRange): string {
    const isActive = range === this.activeRange();
    const baseClasses = 'px-4 py-2 rounded-lg text-sm font-medium transition-all';
    const activeClasses = 'bg-gradient-to-r from-primary-from to-primary-to text-white shadow-md';
    const inactiveClasses = 'bg-card-bg text-text-secondary hover:bg-surface-variant border border-card-border';
    
    return `${baseClasses} ${isActive ? activeClasses : inactiveClasses}`;
  }
}
