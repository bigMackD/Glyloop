import { Component, ChangeDetectionStrategy, signal, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, ActivatedRoute } from '@angular/router';
import { SettingsSectionNavComponent } from './settings-section-nav.component';
import { AccountPreferencesSectionComponent } from './account-preferences-section.component';
import { DataSourcesSectionComponent } from './data-sources-section.component';
import { DisplaySettingsSectionComponent } from './display-settings-section.component';
import { SystemInfoSectionComponent } from './system-info-section.component';
import { SettingsRouteKey } from '../../core/models/settings.types';

/**
 * Settings page component - shell for all settings sections
 * Renders section nav and content; syncs section with route; coordinates initial parallel loads
 */
@Component({
  selector: 'app-settings-page',
  standalone: true,
  imports: [
    CommonModule,
    SettingsSectionNavComponent,
    AccountPreferencesSectionComponent,
    DataSourcesSectionComponent,
    DisplaySettingsSectionComponent,
    SystemInfoSectionComponent
  ],
  templateUrl: './settings-page.component.html',
  styleUrl: './settings-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SettingsPageComponent implements OnInit {
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  // Active section derived from route
  readonly activeSection = signal<SettingsRouteKey>('account');

  // Localized strings
  readonly pageTitle = $localize`:@@settings.title:Settings`;

  ngOnInit(): void {
    // Subscribe to route parameter changes to update active section
    this.route.paramMap.subscribe(params => {
      const section = params.get('section') as SettingsRouteKey | null;
      if (section && this.isValidSection(section)) {
        this.activeSection.set(section);
      } else {
        // Default to account if no section or invalid section
        this.activeSection.set('account');
      }
    });

    // Set initial active section from current route parameter
    const currentSection = this.route.snapshot.paramMap.get('section') as SettingsRouteKey | null;
    if (currentSection && this.isValidSection(currentSection)) {
      this.activeSection.set(currentSection);
    } else {
      // Default to account section
      this.activeSection.set('account');
    }
  }

  /**
   * Handles section navigation
   */
  onNavigate(section: SettingsRouteKey): void {
    this.activeSection.set(section);
    this.router.navigate(['/settings', section]);
  }

  /**
   * Validates if a path is a valid settings section
   */
  private isValidSection(path: string): path is SettingsRouteKey {
    return ['account', 'data-sources', 'display', 'system'].includes(path);
  }
}
