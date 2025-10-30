import { Component, ChangeDetectionStrategy, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatBadgeModule } from '@angular/material/badge';
import { ShellNavLink } from '../models/shell.types';

/**
 * Navigation tabs component for top-level shell navigation.
 * Renders tabs for Dashboard and Settings with active state and attention indicators.
 */
@Component({
  selector: 'nav-tabs',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive, MatIconModule, MatBadgeModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './nav-tabs.component.html',
  styleUrl: './nav-tabs.component.css'
})
export class NavTabsComponent {
  // Inputs
  readonly links = input.required<ShellNavLink[]>();
  readonly activePath = input.required<string>();

  // Outputs
  readonly navigateClick = output<ShellNavLink>();

  /**
   * Determines if a link is active based on the current path
   */
  protected isActive(link: ShellNavLink): boolean {
    const currentPath = this.activePath();

    // Exact match for dashboard
    if (link.id === 'dashboard') {
      return currentPath === '/dashboard';
    }

    // Prefix match for settings (includes nested routes)
    if (link.id === 'settings') {
      return currentPath.startsWith('/settings');
    }

    return false;
  }

  /**
   * Handles tab click
   */
  protected onTabClick(link: ShellNavLink, event: Event): void {
    event.preventDefault();
    this.navigateClick.emit(link);
  }

  /**
   * Handles keyboard navigation
   */
  protected onKeyDown(link: ShellNavLink, event: KeyboardEvent): void {
    if (event.key === 'Enter' || event.key === ' ') {
      event.preventDefault();
      this.navigateClick.emit(link);

      // Move focus to main content after navigation for accessibility
      setTimeout(() => {
        const mainContent = document.querySelector('.app-shell__content') as HTMLElement;
        mainContent?.focus();
      }, 100);
    }
  }
}
