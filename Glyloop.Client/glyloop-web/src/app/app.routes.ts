import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  // Protected routes wrapped in AppShell layout
  {
    path: '',
    loadComponent: () => import('./core/shell/app-shell-layout.component').then(m => m.AppShellLayoutComponent),
    canActivate: [authGuard],
    children: [
      {
        path: 'dashboard',
        loadComponent: () => import('./features/dashboard/dashboard-page.component').then(m => m.DashboardPageComponent),
        data: { title: 'Dashboard' }
      },
      {
        path: 'settings',
        redirectTo: 'settings/data-sources',
        pathMatch: 'full'
      },
      {
        path: 'settings/:section',
        loadComponent: () => import('./features/settings/settings-page.component').then(m => m.SettingsPageComponent),
        data: { title: 'Settings' }
      },
      {
        path: 'settings/data-sources/dexcom/callback',
        loadComponent: () => import('./features/settings/dexcom-callback-page.component').then(m => m.DexcomCallbackPageComponent),
        data: { title: 'Linking Dexcom...' }
      },
      {
        path: '',
        redirectTo: 'dashboard',
        pathMatch: 'full'
      }
    ]
  },
  // Public routes without shell
  {
    path: 'register',
    loadComponent: () => import('./features/auth/register/register-page.component').then(m => m.RegisterPageComponent),
    data: { title: 'Register' }
  },
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login/login-page.component').then(m => m.LoginPageComponent),
    data: { title: 'Login' }
  }
];
