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
