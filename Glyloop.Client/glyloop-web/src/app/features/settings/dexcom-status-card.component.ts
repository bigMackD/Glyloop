import { Component, ChangeDetectionStrategy, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DexcomStatusDto } from '../../core/models/settings.types';

/**
 * Dexcom status card component
 * Displays link state, linked at, token expiry, last sync; status badge
 */
@Component({
  selector: 'app-dexcom-status-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './dexcom-status-card.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DexcomStatusCardComponent {
  readonly status = input.required<DexcomStatusDto | null>();

  /**
   * Formats a date string for display
   */
  formatDate(dateString: string | null): string {
    if (!dateString) return 'N/A';

    try {
      const date = new Date(dateString);
      return date.toLocaleString('en-US', {
        year: 'numeric',
        month: 'short',
        day: 'numeric',
        hour: '2-digit',
        minute: '2-digit'
      });
    } catch {
      return 'Invalid date';
    }
  }

  /**
   * Determines if the token is expiring soon (within 7 days)
   */
  isExpiringSoon(): boolean {
    const status = this.status();
    if (!status?.tokenExpiresAt) return false;

    const expiryDate = new Date(status.tokenExpiresAt);
    const now = new Date();
    const daysUntilExpiry = (expiryDate.getTime() - now.getTime()) / (1000 * 60 * 60 * 24);

    return daysUntilExpiry < 7 && daysUntilExpiry > 0;
  }

  /**
   * Determines if the token has expired
   */
  isExpired(): boolean {
    const status = this.status();
    if (!status?.tokenExpiresAt) return false;

    const expiryDate = new Date(status.tokenExpiresAt);
    const now = new Date();

    return expiryDate < now;
  }
}
