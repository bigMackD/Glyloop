import { Component, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';

/**
 * Display settings section component
 * Read-only note (theme fixed to dark); link to Account for TIR
 */
@Component({
  selector: 'app-display-settings-section',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './display-settings-section.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DisplaySettingsSectionComponent {
  // Theme is fixed to dark mode for now
}
