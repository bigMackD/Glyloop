import { Injectable, signal, computed, inject, effect } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { filter, tap, catchError, of } from 'rxjs';
import {
  ShellStateVM,
  ShellUserSummary,
  ShellNavLink,
  UserMenuItem,
  SessionResponse
} from '../models/shell.types';
import { DexcomStore } from '../stores/dexcom.store';
import { API_CONFIG } from '../config/api.config';

/**
 * Service for managing shell state using Angular signals.
 * Aggregates router info and user profile from existing stores.
 * No HTTP calls for state management; only uses existing facades.
 */
@Injectable({ providedIn: 'root' })
export class ShellStateService {
  private readonly router = inject(Router);
  private readonly http = inject(HttpClient);
  private readonly apiConfig = inject(API_CONFIG);
  private readonly dexcomStore = inject(DexcomStore);

  // Private state signals
  private readonly _user = signal<ShellUserSummary | null>(null);
  private readonly _activePath = signal<string>('/dashboard');
  private readonly _loading = signal<boolean>(false);
  private readonly _error = signal<string | null>(null);

  // Public readonly signals
  readonly user = this._user.asReadonly();
  readonly activePath = this._activePath.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly error = this._error.asReadonly();

  // Computed signal for nav links with attention flags
  readonly navLinks = computed<ShellNavLink[]>(() => {
    const hasDexcomLinked = this._user()?.hasDexcomLinked ?? false;

    return [
      {
        id: 'dashboard',
        label: 'Dashboard',
        path: '/dashboard',
        icon: 'dashboard',
        ariaId: 'nav-dashboard'
      },
      {
        id: 'settings',
        label: 'Settings',
        path: '/settings',
        icon: 'settings',
        ariaId: 'nav-settings',
        requiresAttention: !hasDexcomLinked
      }
    ];
  });

  // User menu items
  readonly userMenuItems = computed<UserMenuItem[]>(() => [
    {
      id: 'account',
      label: 'Account',
      route: '/settings'
    },
    {
      id: 'data-sources',
      label: 'Data Sources',
      route: '/settings/data-sources'
    },
    {
      id: 'system-info',
      label: 'System Info',
      route: '/settings/system'
    },
    {
      id: 'logout',
      label: 'Logout',
      action: 'logout'
    }
  ]);

  // Complete shell state view model
  readonly shellState = computed<ShellStateVM | null>(() => {
    const user = this._user();
    if (!user) return null;

    return {
      user,
      navLinks: this.navLinks(),
      activePath: this._activePath(),
      acknowledgedLegal: true // TODO: Implement legal acknowledgement tracking
    };
  });

  constructor() {
    // Watch router events to update active path
    this.router.events
      .pipe(
        filter((event): event is NavigationEnd => event instanceof NavigationEnd)
      )
      .subscribe((event) => {
        this._activePath.set(event.urlAfterRedirects);
      });

    // Set initial active path
    this._activePath.set(this.router.url);

    // React to Dexcom store changes to update user's hasDexcomLinked status
    effect(() => {
      const isLinked = this.dexcomStore.isLinked();
      const currentUser = this._user();

      if (currentUser && currentUser.hasDexcomLinked !== isLinked) {
        this._user.set({
          ...currentUser,
          hasDexcomLinked: isLinked
        });
      }
    });
  }

  /**
   * Loads the user session from the API
   */
  loadSession(): void {
    this._loading.set(true);
    this._error.set(null);

    const sessionUrl = `${this.apiConfig.baseUrl}/api/auth/session`;

    this.http.get<SessionResponse>(sessionUrl, { withCredentials: true })
      .pipe(
        tap((response) => {
          const user: ShellUserSummary = {
            id: response.userId,
            email: response.email,
            displayName: response.email, // Use email as display name since API doesn't provide a separate name
            hasDexcomLinked: false // Will be updated after Dexcom status loads
          };

          this._user.set(user);
          this._loading.set(false);

          // Load Dexcom status after setting initial user
          // The status will trigger a reactive update via the effect below
          this.dexcomStore.loadStatus();
        }),
        catchError((err) => {
          console.error('Failed to load session:', err);
          this._error.set('Could not load user session. Please try again.');
          this._loading.set(false);
          return of(null);
        })
      )
      .subscribe();
  }

  /**
   * Updates the Dexcom linked status
   * Called after Dexcom linking/unlinking operations
   */
  updateDexcomStatus(isLinked: boolean): void {
    const currentUser = this._user();
    if (currentUser) {
      this._user.set({
        ...currentUser,
        hasDexcomLinked: isLinked
      });
    }
  }

  /**
   * Performs logout by calling the logout endpoint and redirecting to login
   */
  logout(): Promise<void> {
    return new Promise((resolve) => {
      const logoutUrl = `${this.apiConfig.baseUrl}/api/auth/logout`;

      this.http
        .post(logoutUrl, {}, { withCredentials: true })
        .pipe(
          tap(() => {
            // Clear user state
            this._user.set(null);
            // Navigate to login
            this.router.navigate(['/login']);
            resolve();
          }),
          catchError((err) => {
            console.error('Logout failed:', err);
            // Even if logout fails, clear state and redirect
            this._user.set(null);
            this.router.navigate(['/login']);
            resolve();
            return of(null);
          })
        )
        .subscribe();
    });
  }

  /**
   * Navigates to a specific nav link
   */
  navigateTo(link: ShellNavLink): void {
    this.router.navigate([link.path]);
  }

  /**
   * Handles user menu item selection
   */
  handleUserMenuAction(item: UserMenuItem): void {
    if (item.action === 'logout') {
      this.logout();
    } else if (item.route) {
      this.router.navigate([item.route]);
    }
  }
}
