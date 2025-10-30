/**
 * Shell view models and types for AppShell layout
 * Used for top-level navigation, user menu, and shell state management
 */

/**
 * User summary displayed in the shell header
 */
export interface ShellUserSummary {
  id: string;
  displayName: string;
  email: string;
  avatarUrl?: string;
  hasDexcomLinked: boolean;
}

/**
 * Navigation link for top-level shell tabs
 */
export interface ShellNavLink {
  id: 'dashboard' | 'settings';
  label: string;
  path: string;
  icon: string;
  ariaId: string;
  requiresAttention?: boolean;
  badgeCount?: number;
}

/**
 * Complete shell state view model
 */
export interface ShellStateVM {
  user: ShellUserSummary;
  navLinks: ShellNavLink[];
  activePath: string;
  acknowledgedLegal: boolean;
}

/**
 * User menu item in the header dropdown
 */
export interface UserMenuItem {
  id: 'account' | 'data-sources' | 'system-info' | 'logout';
  label: string;
  route?: string;
  action?: 'logout';
}

/**
 * Session response from the auth API (/api/auth/session)
 */
export interface SessionResponse {
  userId: string;
  email: string;
}
