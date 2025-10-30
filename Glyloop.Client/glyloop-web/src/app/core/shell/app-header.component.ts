import { Component, ChangeDetectionStrategy, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatIconModule } from '@angular/material/icon';
import { ShellUserSummary, ShellNavLink, UserMenuItem } from '../models/shell.types';
import { NavTabsComponent } from './nav-tabs.component';
import { UserMenuComponent } from './user-menu.component';

/**
 * Top navigation bar containing brand, nav tabs, and user menu.
 */
@Component({
  selector: 'app-header',
  standalone: true,
  imports: [
    CommonModule,
    MatToolbarModule,
    MatIconModule,
    NavTabsComponent,
    UserMenuComponent
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './app-header.component.html',
  styleUrl: './app-header.component.css'
})
export class AppHeaderComponent {
  // Inputs - all optional to support public pages (login/register) that only show logo
  readonly user = input<ShellUserSummary | null>(null);
  readonly navLinks = input<ShellNavLink[]>([]);
  readonly activePath = input<string>('');
  readonly menuItems = input<UserMenuItem[]>([]);

  // Outputs
  readonly navigate = output<ShellNavLink>();
  readonly menuAction = output<UserMenuItem>();
}
