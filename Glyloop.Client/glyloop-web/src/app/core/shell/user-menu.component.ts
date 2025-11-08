import { Component, ChangeDetectionStrategy, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatDividerModule } from '@angular/material/divider';
import { ShellUserSummary, UserMenuItem } from '../models/shell.types';

/**
 * User menu component with avatar button and dropdown.
 * Provides access to account settings, data sources, system info, and logout.
 */
@Component({
  selector: 'app-user-menu',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatIconModule, MatMenuModule, MatDividerModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './user-menu.component.html',
  styleUrl: './user-menu.component.css'
})
export class UserMenuComponent {
  // Inputs
  readonly user = input.required<ShellUserSummary>();
  readonly items = input.required<UserMenuItem[]>();

  // Outputs
  readonly itemSelect = output<UserMenuItem>();

  /**
   * Handles menu item selection
   */
  protected onItemClick(item: UserMenuItem): void {
    this.itemSelect.emit(item);
  }
}
