import { Component, ChangeDetectionStrategy, input, signal } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';

@Component({
  selector: 'app-session-expired-banner',
  standalone: true,
  imports: [MatIconModule, MatButtonModule],
  templateUrl: './session-expired-banner.component.html',
  styleUrl: './session-expired-banner.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SessionExpiredBannerComponent {
  // Inputs
  readonly message = input<string>($localize`:@@login.banner.sessionExpired:Your session expired. Please log in again.`);

  // Local state for dismissal
  readonly dismissed = signal(false);

  dismiss(): void {
    this.dismissed.set(true);
  }
}
