import { Component, ChangeDetectionStrategy, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SystemInfo } from '../../core/models/settings.types';

/**
 * System info section component
 * Read-only environment info (app version, environment), link to /health
 */
@Component({
  selector: 'app-system-info-section',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './system-info-section.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SystemInfoSectionComponent {
  readonly systemInfo = computed<SystemInfo>(() => ({
    appVersion: '1.0.0',
    environment: 'development',
    healthUrl: '/api/health'
  }));
}
