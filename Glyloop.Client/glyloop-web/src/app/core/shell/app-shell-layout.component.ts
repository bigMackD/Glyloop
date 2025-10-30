import { Component, OnInit, ChangeDetectionStrategy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet } from '@angular/router';
import { ShellStateService } from '../services/shell-state.service';
import { AppHeaderComponent } from './app-header.component';
import { ShellNavLink, UserMenuItem } from '../models/shell.types';

/**
 * Root layout component wrapping protected routes.
 * Orchestrates authenticated navigation, renders header and router outlet,
 * and provides context/services to descendants using existing frontend stores.
 */
@Component({
  selector: 'app-shell-layout',
  standalone: true,
  imports: [CommonModule, RouterOutlet, AppHeaderComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './app-shell-layout.component.html',
  styleUrl: './app-shell-layout.component.css'
})
export class AppShellLayoutComponent implements OnInit {
  protected readonly shellState = inject(ShellStateService);

  ngOnInit(): void {
    // Load user session on component initialization
    this.shellState.loadSession();
  }

  protected onNavigate(link: ShellNavLink): void {
    this.shellState.navigateTo(link);
  }

  protected onMenuAction(item: UserMenuItem): void {
    this.shellState.handleUserMenuAction(item);
  }
}
