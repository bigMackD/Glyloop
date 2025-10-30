import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  {
    path: 'dashboard',
    loadComponent: () => import('./features/dashboard/dashboard-page.component').then(m => m.DashboardPageComponent),
    canActivate: [authGuard],
    data: { title: 'Dashboard' }
  },
  {
    path: 'settings',
    loadComponent: () => import('./features/settings/settings-page.component').then(m => m.SettingsPageComponent),
    canActivate: [authGuard],
    data: { title: 'Settings' }
  },
  {
    path: 'settings/:section',
    loadComponent: () => import('./features/settings/settings-page.component').then(m => m.SettingsPageComponent),
    canActivate: [authGuard],
    data: { title: 'Settings' }
  },
  {
    path: 'dexcom-link',
    loadComponent: () => import('./features/settings/dexcom-callback-page.component').then(m => m.DexcomCallbackPageComponent),
    canActivate: [authGuard],
    data: { title: 'Linking Dexcom...' }
  },
  {
    path: 'register',
    loadComponent: () => import('./features/auth/register/register-page.component').then(m => m.RegisterPageComponent),
    data: { title: 'Register' }
  },
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login/login-page.component').then(m => m.LoginPageComponent),
    data: { title: 'Login' }
  },
  {
    path: '',
    redirectTo: '/dashboard',
    pathMatch: 'full'
  }
];
