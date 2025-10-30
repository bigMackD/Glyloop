import { Component, ChangeDetectionStrategy, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SettingsRouteKey } from '../../core/models/settings.types';

interface NavItem {
  key: SettingsRouteKey;
  label: string;
  icon: string;
}

/**
 * Settings section navigation component
 * Accessible nav (vertical list) to jump to sections with keyboard support
 */
@Component({
  selector: 'app-settings-section-nav',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './settings-section-nav.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SettingsSectionNavComponent {
  readonly active = input.required<SettingsRouteKey>();
  readonly navigate = output<SettingsRouteKey>();

  readonly navItems: NavItem[] = [
    {
      key: 'account',
      label: $localize`:@@settings.nav.account:Account`,
      icon: '👤'
    },
    {
      key: 'data-sources',
      label: $localize`:@@settings.nav.dataSources:Data Sources`,
      icon: '🔗'
    },
    {
      key: 'display',
      label: $localize`:@@settings.nav.display:Display`,
      icon: '🎨'
    },
    {
      key: 'system',
      label: $localize`:@@settings.nav.system:System`,
      icon: '⚙️'
    }
  ];
}
